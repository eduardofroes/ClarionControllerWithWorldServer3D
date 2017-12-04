#!/bin/sh

java -jar WorldServer3D.jar &
sleep 6 

max=10
for i in {1..10}
do
	echo "##### - Starting Experiment $i - #####"
    mono ./ClarionDEMO.exe
    echo "##### - Finishing Experiment $i - #####"
done