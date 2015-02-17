# 1) generate config files 
# 2) push some dependency jars to local maven cache
# 3) build each projects

Invoke-Expression .\config_projects.ps1
pushd lib
.\push_lib_mvn.ps1
popd

pushd docdbgen
mvn clean package
popd

pushd eventgen
mvn clean package
popd

pushd iot
mvn clean package
popd