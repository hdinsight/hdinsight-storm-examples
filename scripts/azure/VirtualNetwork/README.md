# Azure Virtual Networks

## Azure Virtual Network v1 (For HDInsight)
Use the following scripts to create Azure VNet v1 (supported by HDInsight today):
* [CreateVNet.ps1](CreateVNet.ps1) - Create a VNet for examples, you can modify the script or the [hdinsightstormexamples.netcfg](hdinsightstormexamples.netcfg) file to different settings
* [DeleteVNet.ps1](DeleteVNet.ps1) - Removes a VNet (i.e. downloads VNet config, removes one and re-applies the update config)

```DO NOT use Remove-AzureVNetConfig``` command as that will delete all your Virtual Networks.
You should use ```Set-AzureVNetConfig``` command to add/update/remove your particular Virtual Networks fetched via ```Get-AzureVNetConfig```.

You can use ```Get-AzureVNetSite``` to get your VNetID (as an input into HDInsight create clusters script).

```CAUTION:``` These scripts are well tested but please run them on a test subscription or a cluster before making changes to your existing VNet configurations.
(Read the Remove-AzureVNetConfig comment above to understand the risks associated with running wrong VNet commands).

## Azure Virtual Network v2
If you want to create Azure Virtual network v2 (not yet supported by HDInsight):
* [CreateVirtualNetwork.ps1](CreateVirtualNetwork.ps1)
* [DeleteVirtualNetwork.ps1](DeleteVirtualNetwork.ps1)

The Azure Virtual Network v2 is created using Azure Resource Manager. These networks are only visible in the new Azure portal.
