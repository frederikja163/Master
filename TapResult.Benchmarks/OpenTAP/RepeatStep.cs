using OpenTap;

namespace TapResult.Benchmarks.OpenTAP;

[AllowAnyChild]
internal sealed class RepeatStep : TestStep
{
    [Display("Repeat")]
    public int Repeat { get; set; }
    
    public override void Run()
    {
        for (int i = 0; i < Repeat; i++)
        {
            RunChildSteps();
        }
    }
}