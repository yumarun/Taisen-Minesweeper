process_id=$(pgrep -o main)
if [ -z "$process_id" ]; then
	echo "restart go run ."
	/usr/local/go/bin/go run /home/yumaru/TM/server/main.go >> aaa.log &
	echo "finn"
fi
