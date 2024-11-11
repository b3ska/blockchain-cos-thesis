#build 

sudo docker build -t blockchain-node .

#create docker subnet

sudo docker network create --subnet=172.18.0.0/16 blockchain-network

#run 3 nodes

sudo docker run -d --name node1 --net blockchain-network --ip 172.18.0.2 -e NODE_IP=172.18.0.2 -e NODE_PORT=8083 -e NODE_NAME="Node1" -e NODE_KEYWORDS="keywords1" blockchain-node && sudo docker run -d --name node2 --net blockchain-network --ip 172.18.0.3 -e NODE_IP=172.18.0.3 -e NODE_PORT=8081 -e NODE_NAME="Node2" -e NODE_KEYWORDS="keywords2" blockchain-node && sudo docker run -d --name node3 --net blockchain-network --ip 172.18.0.4 -e NODE_IP=172.18.0.4 -e NODE_PORT=8082 -e NODE_NAME="Node3" -e NODE_KEYWORDS="keywords3" blockchain-node