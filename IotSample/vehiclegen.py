import string
import random

def generate(vehicle_count, model_count, vin_length, outfile):
    outf = open(outfile,'w')
    for i in range(0,vehicle_count):
        vin = ''.join(random.choice(string.ascii_uppercase+string.digits) for _ in range(vin_length));
        model = int(random.random() * model_count)
        outf.write(vin+","+str(model)+"\n")
    outf.close()
    
generate(10000, 10, 17, "vehiclevin.txt")