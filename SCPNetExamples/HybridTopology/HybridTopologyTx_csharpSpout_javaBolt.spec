{
  :name "HybridTopologyTx_csharpSpout_javaBolt"
  :topology
    (tx-topology
      "HybridTopologyTx_csharpSpout_javaBolt"

      {
        "generator" 

        (spout-spec 
          (scp-tx-spout
            {
              "plugin.name" "HybridTopology.exe"
              "plugin.args" ["tx-generator"]
              "output.schema" {"default" ["person"]}
              "customized.java.deserializer" ["microsoft.scp.storm.multilang.CustomizedInteropJSONDeserializer" "microsoft.scp.example.HybridTopology.Person"]              
            })

          :p 1)
      }

      {
        "displayer"  

        (bolt-spec
          {
            "generator" :shuffle
          }
          
          (microsoft.scp.example.HybridTopology.TxDisplayer.)

          :p 1)
      })

  :config
    {
      "topology.kryo.register" ["[B"]
    }
}
