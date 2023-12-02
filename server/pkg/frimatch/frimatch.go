package frimatch

/*

・クライアントがroommakeコールを行う
1. サーバー側でroomを作成,
2. roomにクライアントを追加
3. サーバーがroomidを返却
4. クライアントは待ち状態 & 3秒に1回コールを送る
5. サーバー側でtimeout(20秒くらい?) を作成

・クライアントがjoinroomコールを行う
1. roomidが存在するとき
→
1-1. そのroomを削除
1-2. 試合開始
2. roomidが存在しないとき
->「roomidは存在しない」を返却

*/

import (
	"encoding/json"
	"fmt"
	"strconv"

	"math/rand"

	"github.com/gorilla/websocket"
)

var rooms = make(map[string]room)
var ip_password = make(map[string]string)

type clientMessage struct {
	Call string
	Arg  string
}

type room struct {
	clients []*client
}

func newRoom() *room {
	rm := new(room)
	return rm
}

func (rm *room) addClient(c *client) {
	rm.clients = append(rm.clients, c)
}

type client struct {
	conn *websocket.Conn
}

func newClient(conn *websocket.Conn) *client {
	c := new(client)
	c.conn = conn

	return c
}

func Process(conn *websocket.Conn, rawMsg string) {
	fmt.Println("Process() in frimatch.go start.")
	msg := unmarshal(rawMsg)
	if msg.Call == "makeroom" {
		rm, rmId := makeRoom()
		rm.addClient(newClient(conn))
		rooms[rmId] = *rm
		fmt.Println("rmId: ", rmId)
		// conn.SetReadDeadline(time.Now().Add(30 * time.Second))
		ip_password[conn.RemoteAddr().String()] = rmId
		sendRoomMakeFinishedMesssage(conn, rmId)
	} else if msg.Call == "joinroom" {
		fmt.Println("if joinroom.")

		if isRoomExisting(msg.Arg) {
			fmt.Println("len: ", len(rooms[msg.Arg].clients), " msg.Arg: ", msg.Arg)
			sendMatchingOkMessage(conn, rooms[msg.Arg].clients[0].conn)
			deleteRoom(msg.Arg)

		} else {
			fmt.Println("join match failed")
			sendJoinRoomFailedMessage(conn)
		}
		fmt.Println("end if joinroom.")

	}
}

func OnClientDisconnected(ipAddr string) {
	pw := ip_password[ipAddr]
	delete(rooms, pw)
	delete(ip_password, ipAddr)
}

func unmarshal(rawMsg string) clientMessage {
	var cmsg = clientMessage{}
	if err := json.Unmarshal([]byte(rawMsg), &cmsg); err != nil {
		fmt.Println("json deserialize err: ", err)
		return clientMessage{}
	}

	return cmsg
}

func makeRoom() (*room, string) {
	rm := newRoom()
	password := ""
	for i := 0; i < 8; i++ {
		v := rand.Intn(62)
		if v < 10 {
			password += strconv.Itoa(v)
		} else if v < 36 {
			password += string('a' + (v - 10))
		} else {
			password += string('A' + (v - 36))
		}
	}

	return rm, password
}

func sendRoomMakeFinishedMesssage(conn *websocket.Conn, rmId string) {
	if err := conn.WriteMessage(1, []byte("makeroomfinished\n"+rmId)); err != nil {
		fmt.Println("write message error.")
	}
}

func isRoomExisting(passward string) bool {
	_, ok := rooms[passward]
	return ok
}

func deleteRoom(passward string) {
	delete(rooms, passward)
}

func sendMatchingOkMessage(conn1 *websocket.Conn, conn2 *websocket.Conn) {
	if err := conn1.WriteMessage(1, []byte("friendmatchok\n"+conn2.RemoteAddr().String())); err != nil {
		fmt.Println("write message error.")
	}

	if err := conn2.WriteMessage(1, []byte("friendmatchok\n"+conn1.RemoteAddr().String())); err != nil {
		fmt.Println("write message error.")
	}
}

func sendJoinRoomFailedMessage(conn *websocket.Conn) {
	if err := conn.WriteMessage(1, []byte("joinroomfailed")); err != nil {
		fmt.Println("write message error.")
	}
}
