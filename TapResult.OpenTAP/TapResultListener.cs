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
    private ColumnBuilder _id = new ColumnBuilder(LogicalType.String, 16);
    private ColumnBuilder _parent = new ColumnBuilder(LogicalType.String, 16);
    private ColumnBuilder _name = new ColumnBuilder(LogicalType.String, 16);
    
    public void Add(Guid id, Guid? parent, string name)
    {
        _id.WriteString(id.ToString());
        if (parent.HasValue)
        {
            _parent.WriteString(parent.Value.ToString());
        }
        else
        {
            _parent.WriteNull();
        }
        _name.WriteString(name);
    }

    public Table Build()
    {
        return new Table([_id.Build(), _parent.Build(), _name.Build()], ["id", "parent", "name"], "__ancestry__");
    }
}

internal sealed class ParameterBuilder
{
    private readonly ColumnBuilder _id = new ColumnBuilder(LogicalType.String, 16);
    private readonly ColumnBuilder _names = new ColumnBuilder(LogicalType.String, 16);
    private readonly ColumnBuilder _types = new ColumnBuilder(LogicalType.SInt32, 16);
    private readonly ColumnBuilder _values = new ColumnBuilder(LogicalType.String, 16);

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
        _id.WriteString(id.ToString());
        _names.WriteString(name);
        _types.Write((int)type);
        _values.WriteString(value.ToString(CultureInfo.InvariantCulture));
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

    private ParameterBuilder? _paramBuilder = null;
    private AncestryBuilder? _ancestryBuilder = null;
    private readonly object _lock = new object();
    private Writer? _writer = null;
    private ConcurrentBag<Task> _tasks = new ConcurrentBag<Task>();

    public TapResultListener()
    {
        Name = "TapResult";
    }
    
    public override void OnTestPlanRunStart(TestPlanRun planRun)
    {
        base.OnTestPlanRunStart(planRun);
        lock (_lock)
        {
            _ancestryBuilder = new AncestryBuilder();
            _paramBuilder = new ParameterBuilder();
            string path = FilePath.Expand(planRun, planRun.StartTime);
            string dirPath = Path.GetDirectoryName(path) ?? "";
            if (!string.IsNullOrWhiteSpace(dirPath) && !Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
            }
            _writer = new Writer(File.Create(path), leaveOpen: false);
            _tasks.Clear();
        }
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

        Task.WaitAll(_tasks);
        lock (_lock)
        {
            _tasks.Clear();
            _ancestryBuilder = null;
            _paramBuilder = null;
            _writer?.Dispose();
            _writer = null;
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
        Table table = new Table(result.Columns.Select(column => ColumnBuilder.Create(column.Data, out _)),
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
        return nameof(TapResultListener);
    }
}