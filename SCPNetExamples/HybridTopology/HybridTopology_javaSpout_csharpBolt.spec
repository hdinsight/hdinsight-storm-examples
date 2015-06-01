{
  :name "HybridTopology_javaSpout_csharpBolt"
  :topology
    (nontx-topology
      "HybridTopology_javaSpout_csharpBolt"

      {
        "generator" 

        (spout-spec 
          (microsoft.scp.example.HybridTopology.Generator.)           
          :p 1)
      }

      {
        "displayer"  

        (bolt-spec
          {
            "generator" :shuffle
          }

          (scp-bolt
            {
              "plugin.name" "HybridTopology.exe"
              "plugin.args" ["displayer"]
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
