package main

import (
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"strings"

	"yumarun/TM_matching/pkg/battling"
	"yumarun/TM_matching/pkg/matching"

	"github.com/gorilla/websocket"
)

type userInfo struct {
	uid   int
	state string
	conn  *websocket.Conn
}

var users = make(map[string]userInfo)

type ClientMessageForJson struct {
	State   string
	Message string
}

type ClientMessage struct {
	msg  string
	conn *websocket.Conn
}

var clientMsgChan = make(chan ClientMessage)

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool {
		origin := r.Header.Get("Origin")
		return origin == "https://taisen-minesweeper-webglver.web.app"
	},
}

func clientMessageHandler(w http.ResponseWriter, r *http.Request) {
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Println("upgrade error: ", err)
		return
	}

	defer conn.Close()

	for {
		_, message, err := conn.ReadMessage()
		if err != nil {
			log.Println("read error: ", err)
			onClientDisconnected(r.RemoteAddr)

			break
		}

		clientMsgChan <- ClientMessage{msg: string(message), conn: conn}

	}
}

func echoAllMessage() {
	for {

		client_str := <-clientMsgChan

		arr := strings.Split(client_str.msg, "\n")
		ipAddr := client_str.conn.RemoteAddr().String()

		// log.Println("arr0: ", arr[0], " arr1: ", arr[1], " from: ", client_str.conn.RemoteAddr().String())

		// もし「マッチしたい」というメッセージなら
		if arr[0] == "matching" {
			var rawClientMsg ClientMessageForJson
			if err := json.Unmarshal([]byte(arr[1]), &rawClientMsg); err != nil {
				fmt.Println("json deserialize err: ", err)
				return
			}

			// mapに登録されているか確認, なかったら登録
			user, isExistingUser := users[ipAddr]
			if !isExistingUser {
				uid := len(users)
				user = userInfo{uid: uid, state: rawClientMsg.State, conn: client_str.conn}
				users[ipAddr] = user
			}

			// userのstateを更新
			updateUserState(ipAddr, "matching")
			matching.Process(user.uid, ipAddr, user.conn)
		} else if arr[0] == "battling" {
			updateUserState(ipAddr, "battling")
			battling.Process(arr[1], client_str.conn)
		}

		fmt.Println("user state: ", users[client_str.conn.RemoteAddr().String()].state)
	}
}

// users・matchingpoolからuserを削除
func onClientDisconnected(ipAddr string) {

	// usersに登録されていない場合，return
	if !registeredInUsers(ipAddr) {

		return
	}

	// if matchingの途中

	if users[ipAddr].state == "matching" {
		matching.CancelMatching(ipAddr)
		delete(users, ipAddr)

		log.Println(ipAddr, " disconnected during matching.")
	} else if users[ipAddr].state == "battling" { // if 試合中
		delete(users, ipAddr)
		battling.OnClientDisconnected(ipAddr)
		log.Println(ipAddr, " disconnected during battling.")
	}

}

func registeredInUsers(ipAddr string) bool {
	_, ok := users[ipAddr]
	return ok
}

func updateUserState(ipAddr string, state string) {
	var user = users[ipAddr]
	user.state = state
	users[ipAddr] = user
}

func main() {
	fmt.Println("server start...")
	http.HandleFunc("/ws", clientMessageHandler)

	go echoAllMessage()

	if err := http.ListenAndServeTLS(":8080", "/etc/letsencrypt/archive/tmserver.yumarun.net/fullchain1.pem", "/etc/letsencrypt/archive/tmserver.yumarun.net/privkey1.pem", nil); err != nil {
		log.Fatal("ListenANDServe: ", err)
	}
}
