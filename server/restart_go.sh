process_id=$(pgrep -o TM_matching)
if [ -z "$process_id" ]; then
	echo "restart go run ."
	/home/yumaru/TM/server/TM_matching >> aaa.log &
	echo "fin"
fi
