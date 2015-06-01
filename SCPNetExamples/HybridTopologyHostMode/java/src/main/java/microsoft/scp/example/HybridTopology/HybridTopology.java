package microsoft.scp.example.HybridTopology;

import backtype.storm.Config;
import backtype.storm.LocalCluster;
import backtype.storm.StormSubmitter;
import backtype.storm.topology.TopologyBuilder;

/**
 * Created by tqin on 9/11/2014.
 */
public class HybridTopology {
    public static void main(String[] args) throws Exception {

        TopologyBuilder builder = new TopologyBuilder();

        builder.setSpout("generator", new Generator(1, "test", null), 1);
        builder.setBolt("displayer", new Displayer(1, "test", null), 1).shuffleGrouping("generator");

        Config conf = new Config();
        conf.setDebug(true);


        if (args != null && args.length > 0) {
            conf.setNumWorkers(1);

            StormSubmitter.submitTopology(args[0], conf, builder.createTopology());
        }
        else {
            conf.setMaxTaskParallelism(3);

            LocalCluster cluster = new LocalCluster();
            cluster.submitTopology("HybridTopology_java", conf, builder.createTopology());

            Thread.sleep(10000);

            cluster.shutdown();
        }
    }
}
