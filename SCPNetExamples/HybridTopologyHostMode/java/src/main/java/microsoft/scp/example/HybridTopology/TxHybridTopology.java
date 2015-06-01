package microsoft.scp.example.HybridTopology;

import backtype.storm.Config;
import backtype.storm.LocalCluster;
import backtype.storm.StormSubmitter;
import backtype.storm.transactional.TransactionalTopologyBuilder;

/**
 * Created by tqin on 10/29/2014.
 */
public class TxHybridTopology {
    public static void main(String[] args) throws Exception {
        TxGenerator generator = new TxGenerator(100, "test", null);

        TransactionalTopologyBuilder builder = new TransactionalTopologyBuilder("HybridTopologyTx_java", "tx-generator", generator, 1);
        builder.setBolt("tx-displayer", new TxDisplayer(100, "test", null), 1).shuffleGrouping("tx-generator");

        Config conf = new Config();
        conf.setDebug(true);

        if (args != null && args.length > 0) {
            conf.setNumWorkers(1);
            StormSubmitter.submitTopology(args[0], conf, builder.buildTopology());
        }
        else {
            conf.setMaxTaskParallelism(3);
            LocalCluster cluster = new LocalCluster();
            cluster.submitTopology("HybridTopologyTx_java", conf, builder.buildTopology());
            Thread.sleep(10000);
            cluster.shutdown();
        }
    }
}
