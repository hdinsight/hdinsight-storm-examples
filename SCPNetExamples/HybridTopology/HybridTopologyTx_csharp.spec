{
  :name "HybridTopologyTx_csharp"
  :topology
    (tx-topology
      "HybridTopologyTx_csharp"

      {
        "generator" 

        (spout-spec 
          (scp-tx-spout
            {
              "plugin.name" "HybridTopology.exe"
              "plugin.args" ["tx-generator"]
              "output.schema" {"default" ["person"]}
            })

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
            })

          :p 1)
      })

  :config
    {
      "topology.kryo.register" ["[B"]
    }
}
