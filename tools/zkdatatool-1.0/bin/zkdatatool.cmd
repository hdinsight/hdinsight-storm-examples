@echo off
java -cp .;..\lib\*;..\target\*;%HADOOP_HOME%\share\hadoop\common\*;%HADOOP_HOME%\share\hadoop\common\lib\*;%HADOOP_HOME%\share\hadoop\hdfs\*;%HADOOP_HOME%\share\hadoop\hdfs\lib\*;%HADOOP_HOME%\etc\hadoop;%FC_HOME%\conf com.microsoft.storm.zkdatatool.ZkdataImporter %* 
@echo on