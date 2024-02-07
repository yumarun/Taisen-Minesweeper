process_id=$(pgrep -o TM_apiserver)
if [ -z "$process_id" ]; then
	echo "restart go r ."
	/home/ubuntu/TM_api/api_server/TM_apiserver >> aaa.log &
	echo "finnn"
fi
