# OpenTAP result file format

This is currently work in progress.


## Setting up Spark

First install: **[Apache Spark 2.4.1](https://archive.apache.org/dist/spark/spark-2.4.1/spark-2.4.1-bin-hadoop2.7.tgz)**

If you are using Linux then first run `dotnet restore`, then `./LinuxFixSparkNuget.sh` to fix the Spark nuget package. Then invalidate IDE cache if necessary.

Finally you should be able to run the Spark benchmarks with the run profile `Linux Run Spark Benchmarks`

Based off the following guides:

Windows: https://github.com/dotnet/spark/blob/main/docs/getting-started/windows-instructions.md

Ubuntu: https://github.com/dotnet/spark/blob/main/docs/getting-started/ubuntu-instructions.md

# Authors

Authors @Aavild and @frederikja163 for their masters project at AAU