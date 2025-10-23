# IV.DataProvider.WebAPI

// Open cmd
// Go to folder where Dockerfile is existing

// Command to build image using PAT for NuGet.config

docker build --build-arg PAT=<Put_PAT_here> -t iv.dataprovider.webapp.services .

docker build --no-cache --build-arg PAT=<Put_PAT_here> -t iv.dataprovider.webapp.services .

// Command to run container

docker run -d --name iv.dataprovider.webapp.services.container -p 8080:8080 -p 8081:8081 iv.dataprovider.webapp.services

// Command to stop/start/restart container
docker stop iv.dataprovider.webapp.services.container
docker start iv.dataprovider.webapp.services.container
docker restart iv.dataprovider.webapp.services.container

// Command to remove container
docker rm iv.dataprovider.webapp.services.container

// Command to remove container forcefully
docker rm -f iv.dataprovider.webapp.services.container

// Command to remove image
docker rmi iv.dataprovider.webapp.services

// Command to remove image forcefully
docker rmi -f iv.dataprovider.webapp.services
