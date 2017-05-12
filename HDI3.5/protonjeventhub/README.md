This is the latest Storm EventHub jars which make use of the protonj protocol to interact with EventHub

To install it to your local maven repo:
mvn install:install-file -Dfile=storm-eventhubs-2.0.0-SNAPSHOT-jar-with-dependencies.jar -DgroupId=org.apache.storm -DartifactId=storm-eventhubs -Dversion=2.0.0-SNAPSHOT -Dpackaging=jar

Since the newer sdk uses java 8 these jars will run on 3.5 HDInsight clusters onwards

