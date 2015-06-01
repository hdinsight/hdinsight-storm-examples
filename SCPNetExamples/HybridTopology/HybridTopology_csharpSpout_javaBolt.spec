{
  :name "HybridTopology_charpSpout_javaBolt"
  :topology
    (nontx-topology
      "HybridTopology_charpSpout_javaBolt"

      {
        "generator" 

        (spout-spec 
          (scp-spout
            {
              "plugin.name" "HybridTopology.exe"
              "plugin.args" ["generator"]
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

          (microsoft.scp.example.HybridTopology.Displayer.)

          :p 1)
      })

  :config
    {
      "topology.kryo.register" ["[B"]
    }
}
