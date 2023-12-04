PATH=/home/yumaru/.vscode-server/bin/7f329fe6c66b0f86ae1574c2911b681ad5a45d63/bin/remote-cli:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/games:/usr/local/games:/snap/bin:/usr/local/go/bin:/usr/local/go/bin

echo "restart_go.sh start!"
process_id=$(pgrep -o main)
if [ -z "$process_id" ]; then
	echo "restart go run ."
	cd /home/yumaru/taisenMsPub/server
	go run /home/yumaru/taisenMsPub/server/main.go >> aaa.log &
	echo "finn"
fi
