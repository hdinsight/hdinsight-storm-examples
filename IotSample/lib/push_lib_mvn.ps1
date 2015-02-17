# EventHubSpout jar
cmd /c "mvn install:install-file -Dfile=../../lib/eventhubs-storm-spout-0.9-jar-with-dependencies.jar -DgroupId=com.microsoft.eventhubs -DartifactId=eventhubs-storm-spout -Dversion=0.9 -Dpackaging=jar"

cmd /c "mvn org.apache.maven.plugins:maven-install-plugin:2.5.2:install-file -Dfile=hadoop-azure-3.0.0-SNAPSHOT.jar"
cmd /c "mvn org.apache.maven.plugins:maven-install-plugin:2.5.2:install-file -Dfile=hadoop-client-3.0.0-SNAPSHOT.jar"
cmd /c "mvn org.apache.maven.plugins:maven-install-plugin:2.5.2:install-file -Dfile=hadoop-hdfs-3.0.0-SNAPSHOT.jar"

# Push dependencies to local repo so our uber jar includes all dependencies
cmd /c "mvn org.apache.maven.plugins:maven-install-plugin:2.5.2:install-file -Dfile=hadoop-common-3.0.0-SNAPSHOT.jar -DpomFile=hadoop-common-3.0.0-SNAPSHOT.pom"
cmd /c "mvn org.apache.maven.plugins:maven-install-plugin:2.5.2:install-file -Dfile=hadoop-project-dist-3.0.0-SNAPSHOT.pom -DpomFile=hadoop-project-dist-3.0.0-SNAPSHOT.pom"
cmd /c "mvn org.apache.maven.plugins:maven-install-plugin:2.5.2:install-file -Dfile=hadoop-project-3.0.0-SNAPSHOT.pom -DpomFile=hadoop-project-3.0.0-SNAPSHOT.pom"
cmd /c "mvn org.apache.maven.plugins:maven-install-plugin:2.5.2:install-file -Dfile=hadoop-main-3.0.0-SNAPSHOT.pom -DpomFile=hadoop-main-3.0.0-SNAPSHOT.pom"