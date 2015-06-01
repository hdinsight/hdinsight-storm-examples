package microsoft.scp.example.HybridTopology;

import backtype.storm.task.OutputCollector;
import backtype.storm.task.TopologyContext;
import backtype.storm.topology.OutputFieldsDeclarer;
import backtype.storm.topology.base.BaseRichBolt;
import backtype.storm.tuple.Tuple;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Created by tqin on 9/11/2014.
 */
public class Displayer extends BaseRichBolt {
    public static final Logger LOG = LoggerFactory.getLogger(Displayer.class);

    // input parameters are only used to test serialization/deserialization of constructor parameters between C# and Java
    public Displayer(int param1, String param2, String param3) {
        LOG.info("Displayer's constructor is called");
        LOG.info("param1: " + param1);

        if (param2 != null) {
            LOG.info("param2: " + param2);
        } else {
            LOG.info("param2: NULL");
        }

        if (param3 != null) {
            LOG.info("param3: " + param3);
        } else {
            LOG.info("param3: NULL");
        }
    }

    private OutputCollector _collector;

    @Override
    public void prepare(Map map, TopologyContext topologyContext, OutputCollector outputCollector) {
        _collector = outputCollector;
    }

    @Override
    public void execute(Tuple tuple) {
        Person person = (Person)tuple.getValue(0);
        LOG.info("person: " + person.toString());
    }

    @Override
    public void declareOutputFields(OutputFieldsDeclarer outputFieldsDeclarer) {
    }
}
