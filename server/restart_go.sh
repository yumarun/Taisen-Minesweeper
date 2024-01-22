#PATH=/home/yumaru/.vscode-server/bin/0ee08df0cf4527e40edc9aa28f4b5bd38bbff2b2/bin/remote-cli:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/games:/usr/local/games:/snap/bin:/home/yumaru/.dotnet/tools:/usr/local/go/bin:/usr/local/go/bin

process_id=$(pgrep -o main)
if [ -z "$process_id" ]; then
	echo "restart go run ."
	cd /home/yumaru/TM/server
	#go run /home/yumaru/TM/server/main.go >> aaa.log &
	echo "finn"
fi
