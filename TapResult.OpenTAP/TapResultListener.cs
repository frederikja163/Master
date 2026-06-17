using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenTap;

namespace TapResult.OpenTAP;

internal sealed class AncestryBuilder
{
    private readonly ColumnBuilder<string> _id = new(16);
    private readonly ColumnBuilder<string> _parent = new (16);
    private readonly ColumnBuilder<string> _name = new (16);
    
    public void Add(Guid id, Guid? parent, string name)
    {
        _id.WriteValue(id.ToString());
        if (parent.HasValue)
        {
            _parent.WriteValue(parent.Value.ToString());
        }
        else
        {
            _parent.WriteValue(string.Empty);
        }
        _name.WriteValue(name);
    }

    public Table Build()
    {
        return new Table([_id.Build(), _parent.Build(), _name.Build()], ["id", "parent", "name"], "__ancestry__");
    }
}

internal sealed class ParameterBuilder
{
    private readonly ColumnBuilder<string> _id = new(16);
    private readonly ColumnBuilder<string> _names = new(16);
    private readonly ColumnBuilder<int> _types = new(16);
    private readonly ColumnBuilder<string> _values = new(16);

    public void Add(TestRun run)
    {
        foreach (ResultParameter parameter in run.Parameters)
        {
            Add(run.Id, string.Join('/', parameter.Group, parameter.Name), parameter.Value.GetTypeCode(), parameter.Value);
        }
        Add(run.Id, nameof(run.Duration), TypeCode.String, run.Duration.ToString("G", CultureInfo.InvariantCulture));
        Add(run.Id, nameof(run.Verdict), TypeCode.String, run.Verdict.ToString());
        Add(run.Id, nameof(run.StartTime), TypeCode.String, run.StartTime.ToString("G", CultureInfo.InvariantCulture));
    }
    
    private void Add(Guid id, string name, TypeCode type, IConvertible value)
    {
        _id.WriteValue(id.ToString());
        _names.WriteValue(name);
        _types.WriteValue((int)type);
        _values.WriteValue(value.ToString(CultureInfo.InvariantCulture));
    }

    public Table Build()
    {
        return new Table([_id.Build(), _names.Build(), _types.Build(), _values.Build()], ["id", "names", "type", "value"], "__params__");
    }
}

[Display("TapResult", "Writes results to .TapResult files.", "Database")]
public sealed class TapResultListener : ResultListener
{
    [Display("Path", "The path to the file.")]
    public MacroString FilePath { get; set; } = new MacroString
    {
        Text = "Results/<Date>-<Verdict>.TapResult",
    };

    public Func<string, WriterBase> WriterCreator { get; set; } = (path) => new TapResultWriter(path);

    private ParameterBuilder? _paramBuilder = null;
    private AncestryBuilder? _ancestryBuilder = null;
    private readonly object _lock = new object();
    private WriterBase? _writer = null;
    private ConcurrentBag<Task> _tasks = new ConcurrentBag<Task>();

    public TapResultListener()
    {
        Name = "TapResult";
    }

    public override void Close()
    {
        base.Close();
        Task.WaitAll(_tasks);
        _tasks.Clear();
        _ancestryBuilder = null;
        _paramBuilder = null;
        if (_writer is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public override void OnTestPlanRunStart(TestPlanRun planRun)
    {
        base.OnTestPlanRunStart(planRun);
        _ancestryBuilder = new AncestryBuilder();
        _paramBuilder = new ParameterBuilder();
        string path = FilePath.Expand(planRun, planRun.StartTime);
        string dirPath = Path.GetDirectoryName(path) ?? "";
        if (!string.IsNullOrWhiteSpace(dirPath) && !Directory.Exists(Path.GetDirectoryName(path)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
        }

        _writer = WriterCreator(path);
        _tasks.Clear();
    }

    public override void OnTestPlanRunCompleted(TestPlanRun planRun, Stream logStream)
    {
        base.OnTestPlanRunCompleted(planRun, logStream);

        if (_ancestryBuilder is not null)
        {
            _ancestryBuilder.Add(planRun.Id, null, planRun.TestPlanName);
            _tasks.Add(WriteAndCompressAsync(_ancestryBuilder.Build()));
        }

        if (_paramBuilder is not null)
        {
            _paramBuilder.Add(planRun);
            _tasks.Add(WriteAndCompressAsync(_paramBuilder.Build()));
        }
    }

    public override void OnTestStepRunCompleted(TestStepRun stepRun)
    {
        base.OnTestStepRunCompleted(stepRun);
        _paramBuilder?.Add(stepRun);
        _ancestryBuilder?.Add(stepRun.TestStepId, stepRun.Id, stepRun.TestStepName);
    }

    public override void OnResultPublished(Guid stepRunId, ResultTable result)
    {
        base.OnResultPublished(stepRunId, result);
        Table table = new Table(result.Columns.Select(column => ColumnBuilder.Create(column.Data)),
            result.Columns.Select(col => col.Name), string.Join('/', stepRunId, result.Name));
        _tasks.Add(WriteAndCompressAsync(table));
    }

    public async Task WriteAndCompressAsync(Table table)
    {
        await table.CompressAsync();
        lock (_lock)
            _writer?.Write(table);
    }

    public override string ToString()
    {
        return Name;
    }
}