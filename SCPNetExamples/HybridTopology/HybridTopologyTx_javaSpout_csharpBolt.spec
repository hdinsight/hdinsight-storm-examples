{
  :name "HybridTopologyTx_javaSpout_csharpBolt"
  :topology
    (tx-topology
      "HybridTopologyTx_javaSpout_csharpBolt"

      {
        "generator" 

        (spout-spec
          (microsoft.scp.example.HybridTopology.TxGenerator.)
          :p 1)
      }

      {
        "displayer"  

        (bolt-spec
          {
            "generator" :shuffle
          }

          (scp-tx-commit-bolt
            {
              "plugin.name" "HybridTopology.exe"
              "plugin.args" ["tx-displayer"]
              "output.schema" {}
              "customized.java.serializer" ["microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer"]              
            })

          :p 1)
      })

  :config
    {
      "topology.kryo.register" ["[B"]
    }
}
