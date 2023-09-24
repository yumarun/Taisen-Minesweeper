package matching

import (
	"fmt"

	"github.com/gorilla/websocket"
)

type userInfo struct {
	uid    int
	ipAddr string
	conn   *websocket.Conn
}

var users = make(map[string]userInfo)
var beInPool = make(map[string]bool)
var usrNumInPool = 0

func Process(uid int, ipAddr string, conn *websocket.Conn) {
	if !isUserRegistered(ipAddr) {
		registerUser(ipAddr, userInfo{uid: uid, ipAddr: ipAddr, conn: conn})
	}

	usr := users[ipAddr]

	if !isUserInPool(usr) {
		addUserToPool(usr)
	}

	ok, usr1, usr2 := matchmakeFromPool()
	if ok {
		okMatching(usr1, usr2)
	} else {
		failMatching(conn)
	}

}

func isUserRegistered(ipAddr string) bool {
	_, ok := users[ipAddr]
	return ok
}

func registerUser(ipAddr string, ui userInfo) {
	users[ipAddr] = ui
}

func isUserInPool(usr userInfo) bool {
	_, ok := beInPool[usr.ipAddr]
	return ok
}

func addUserToPool(usr userInfo) {
	beInPool[usr.ipAddr] = true
	usrNumInPool++
}

func matchmakeFromPool() (bool, userInfo, userInfo) {
	if usrNumInPool < 2 {
		return false, userInfo{}, userInfo{}
	}

	var twoUsrs []string
	for key := range beInPool {
		twoUsrs = append(twoUsrs, key)
	}

	return true, users[twoUsrs[0]], users[twoUsrs[1]]
}

func failMatching(conn *websocket.Conn) {
	if err := conn.WriteMessage(1, []byte("match fail...")); err != nil {
		fmt.Println("fail matchi err: ", err)
	}

}

func okMatching(usr1 userInfo, usr2 userInfo) {

	fmt.Println("match decided! ip1: ", usr1.ipAddr, " ip2: ", usr2.ipAddr)

	usrNumInPool -= 2
	delete(beInPool, usr1.ipAddr)
	delete(beInPool, usr2.ipAddr)

	if err := usr1.conn.WriteMessage(1, []byte("match!op:\n"+usr2.ipAddr)); err != nil {
		fmt.Println("ok match err: ", err)
	}

	if err := usr2.conn.WriteMessage(1, []byte("match!op:\n"+usr1.ipAddr)); err != nil {
		fmt.Println("ok match err: ", err)
	}
}

func CancelMatching(ipAddr string) {
	usrNumInPool--
	delete(beInPool, ipAddr)
	delete(users, ipAddr)
	fmt.Println("ip: ", ipAddr, " canceled match. ")
}
