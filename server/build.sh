

msbuild Server.sln /p:Configuration=Release /p:Platform="x86"

cd ./Server/bin/Release/
mono server.exe
cd ./../../../
