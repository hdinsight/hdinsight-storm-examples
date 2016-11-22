# Storm-Eventhubs jar

The storm-eventhubs-1.0.3-jar-with-dependencies.jar is a compiled version of latest code (as of Nov 1 2016) from [Apache Storm's 1.0.x branch] (https://github.com/apache/storm/tree/1.0.x-branch/external/storm-eventhubs)

Please run the below command to install it to local maven repo:

 mvn install:install-file -Dfile=C:\git\hdinsight-storm-examples\hdi3.5\HDI3.5\lib\storm-eventhubs-1.0.3-jar-with-dependencies.jar -DgroupId=org.apache.storm -DartifactId=storm-eventhubs -Dversion=1.0.3 -Dpackaging=jar
