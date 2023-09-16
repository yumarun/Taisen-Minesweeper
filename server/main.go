package main

import (
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"strconv"
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
		return true
	},
}

func echo(w http.ResponseWriter, r *http.Request) {
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Println("upgrade error:", err)
		return
	}

	defer conn.Close()

	for {
		messageType, message, err := conn.ReadMessage()
		if err != nil {
			log.Println("read error:", err)
			break
		}

		log.Printf("recv: %s from %s, ", message, r.RemoteAddr)
		if err = conn.WriteMessage(messageType, message); err != nil {
			log.Println("write error: ", err)
			break
		}
	}
}

func clientMessageHandler(w http.ResponseWriter, r *http.Request) {
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Println("upgrade error: ", err)
		return
	}

	defer conn.Close()

	for {
		messageType, message, err := conn.ReadMessage()
		if err != nil {
			log.Println("read error: ", err)
			onClientDisconnected(r.RemoteAddr)

			break
		}
		log.Printf("received %s, type: %s from %s", message, strconv.Itoa(messageType), r.RemoteAddr)

		clientMsgChan <- ClientMessage{msg: string(message), conn: conn}

	}
}

func echoAllMessage() {
	for {

		client_str := <-clientMsgChan

		log.Println(client_str.msg)

		arr := strings.Split(client_str.msg, "\n")

		// もし「マッチしたい」というメッセージなら
		if arr[0] == "matching" {
			var rawClientMsg ClientMessageForJson
			if err := json.Unmarshal([]byte(arr[1]), &rawClientMsg); err != nil {
				fmt.Println("json deserialize err: ", err)
				return
			}

			ipAddr := client_str.conn.RemoteAddr().String()

			// mapに登録されているか確認, なかったら登録
			user, isExistingUser := users[ipAddr]
			if !isExistingUser {
				uid := len(users)
				user = userInfo{uid: uid, state: rawClientMsg.State, conn: client_str.conn}
				users[ipAddr] = user
			}

			// userのstateを更新
			updateUserState(ipAddr, rawClientMsg.State)
			matching.Process(user.uid, ipAddr, user.conn)
		} else if arr[0] == "battling" {
			battling.Process(arr[1], client_str.conn)
		}

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
	}

	// if 試合中
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
	http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		fmt.Fprintf(w, "welcome to my website!")
	})

	http.HandleFunc("/ws", clientMessageHandler)
	http.HandleFunc("/ws2", echo)

	go echoAllMessage()

	if err := http.ListenAndServe(":8080", nil); err != nil {
		log.Fatal("ListenANDServe: ", err)
	}
}
