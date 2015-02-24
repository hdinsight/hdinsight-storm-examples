# 1) generate config files 
# 2) push some dependency jars to local maven cache
# 3) build each projects

.\config_projects.ps1
pushd lib
.\push_lib_mvn.ps1
popd

pushd EventCountTopology
mvn clean package
popd

pushd EventGenTopology
mvn clean package
popd
