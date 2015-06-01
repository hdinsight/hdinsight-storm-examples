package microsoft.scp.example.HybridTopology;

import backtype.storm.coordination.BatchOutputCollector;
import backtype.storm.task.TopologyContext;
import backtype.storm.topology.OutputFieldsDeclarer;
import backtype.storm.topology.base.BaseTransactionalBolt;
import backtype.storm.transactional.ICommitter;
import backtype.storm.transactional.TransactionAttempt;
import backtype.storm.tuple.Tuple;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Created by tqin on 10/29/2014.
 */
public class TxDisplayer extends BaseTransactionalBolt implements ICommitter {
    public static final Logger LOG = LoggerFactory.getLogger(Displayer.class);

    @Override
    public void prepare(Map conf, TopologyContext context, BatchOutputCollector collector, TransactionAttempt attempt) {
        LOG.info("prepare, attempt: " + attempt);
    }

    @Override
    public void execute(Tuple tuple) {
        LOG.info("execute enter");
        Person person = (Person)tuple.getValue(1);
        LOG.info("person: " + person.toString());
        LOG.info("execute exit");
    }

    @Override
    public void finishBatch() {
        LOG.info("finishBatch");
    }

    @Override
    public void declareOutputFields(OutputFieldsDeclarer outputFieldsDeclarer) {
    }
}
