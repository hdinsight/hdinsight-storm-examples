{
  :name "HelloWorld"
  :topology
    (nontx-topology
      "HelloWorld"

      {
        "generator" 

        (spout-spec 
          (scp-spout
            {
              "plugin.name" "SCPHost.exe"
              "plugin.args" ["HelloWorld.dll" "Scp.App.HelloWorld.Generator" "Get" "HelloWorld.dll.config"]
              "output.schema" {"default" ["sentence"]}
            })
           
          :p 1)
      }

      {
        "splitter"  

        (bolt-spec
          {
            "generator" :shuffle
          }

          (scp-bolt
            {
              "plugin.name" "SCPHost.exe"
              "plugin.args" ["HelloWorld.dll" "Scp.App.HelloWorld.Splitter" "Get"]
             "output.schema" {"default" ["word" "firstLetterOfWord"]}
            })

          :p 1)

        "counter"  

        (bolt-spec
          {
            "splitter" (scp-field-group :non-tx [1])
          }

          (scp-bolt
            {
              "plugin.name" "SCPHost.exe"
              "plugin.args" ["HelloWorld.dll" "Scp.App.HelloWorld.Counter" "Get"]
              "output.schema" {"default" ["word" "count"]}
            })

          :p 1)
      })

  :config
    {
      "topology.kryo.register" ["[B"]
    }
}
