# OpenTAP result file format

This is currently work in progress.


## Setting up Spark

First install: **[Apache Spark 3.5.3](https://archive.apache.org/dist/spark/spark-3.5.3/)** - spark-3.5.3-bin-hadoop3.tgz is recommended and ensure that it aligns with the path in `Start spark debug mode`

Then install java 17 as per the [docs](https://archive.apache.org/dist/spark/docs/3.5.3/)

If you are using Linux then first run `dotnet restore`, then `./LinuxFixSparkNuget.sh` to fix the Spark nuget package. Then invalidate IDE cache if necessary.

Then you should be able to run Spark with the run profile `Start spark debug mode`

When it says "* .NET Backend running debug mode. Press enter to exit *", you can start the benchmarks as usual.

# Authors

Authors @Aavild and @frederikja163 for their masters project at AAU