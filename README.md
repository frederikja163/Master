# OpenTAP result file format

This is currently work in progress.


## Setting up Spark

First install: **[Apache Spark 3.5.3](https://archive.apache.org/dist/spark/spark-3.5.3/)**

If you are using Linux then first run `dotnet restore`, then `./LinuxFixSparkNuget.sh` to fix the Spark nuget package. Then invalidate IDE cache if necessary.

Then you should be able to run Spark with the run profile `Start spark debug mode`

When it says "* .NET Backend running debug mode. Press enter to exit *", you can start the benchmarks as usual.

## Benchmarks

Can be run with `Master.Benchmarks` run profile and results can be found in `Master.Benchmarks/bin/Release/net9.0/BenchmarkDotNet.Artifacts/results`.

# Authors

Authors @Aavild and @frederikja163 for their masters project at AAU