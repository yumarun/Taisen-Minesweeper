package battling

import (
	"encoding/json"
	"fmt"
	"sort"

	"github.com/gorilla/websocket"
)

var allMatches = make(map[string]*match)
var matchNumCount = 0
var boardWidth = 15

type msgFromClientJsonFormat struct {
	LatestBoard       []int
	IsLosed           bool
	LatestMsgNum      int
	LatestAttackPoint int
	OpponentAddr      string
}

type msgToClientJsonFormat struct {
	LatestBoard       []int
	IsLosed           bool
	LatestMsgNum      int
	LatestAttackPoint int
	OpponentAddr      string
}

type match struct {
	matchID int
	uindex  map[string]int
	clients [2]clientCondition
}

type clientCondition struct {
	latestBoard       []int
	isLosed           bool
	latestMsgNum      int
	latestAttackPoint int
	conn              *websocket.Conn
	isReady           bool
}

func initMatch(ip1 string, ip2 string) {
	matchNumCount++
	newMatchID := getNewMatchID()

	var uindex = make(map[string]int)
	setNewUindex(uindex, ip1, ip2)

	newMatch := match{
		matchID: newMatchID,
		uindex:  uindex,
		clients: [2]clientCondition{},
	}

	allMatches[getSortedAddrs(ip1, ip2)] = &newMatch
}

func Process(rawMsg string, conn *websocket.Conn) {

	c := msgFromClientJsonFormat{}
	deserializeMessage(rawMsg, &c)

	myIpAddr := conn.RemoteAddr().String()

	if !isMatchRegistered(myIpAddr, c.OpponentAddr) {
		initMatch(myIpAddr, c.OpponentAddr)
	}

	myCond := clientCondition{
		latestBoard:       c.LatestBoard,
		isLosed:           c.IsLosed,
		latestMsgNum:      c.LatestMsgNum,
		latestAttackPoint: c.LatestAttackPoint,
		conn:              conn,
		isReady:           true,
	}

	joiningMatch := allMatches[getSortedAddrs(myIpAddr, c.OpponentAddr)]
	myUindex := joiningMatch.uindex[myIpAddr]

	fmt.Println("addr: ", myIpAddr, " uindex: ", myUindex)

	setMyCondition(
		myCond,
		&joiningMatch.clients[myUindex],
	)

	if joiningMatch.clients[1-myUindex].isReady {
		sendOpponentCondition(joiningMatch.clients[1-myUindex], conn)

	} else {
		// TODO
	}

}

func getSortedAddrs(addr1 string, addr2 string) string {
	addrs := []string{addr1, addr2}
	sort.Strings(addrs)
	return addrs[0] + addrs[1]
}

func isMatchRegistered(addr1 string, addr2 string) bool {
	_, ok := allMatches[getSortedAddrs(addr1, addr2)]
	return ok
}

func deserializeMessage(msg string, c *msgFromClientJsonFormat) {
	if err := json.Unmarshal([]byte(msg), &c); err != nil {
		fmt.Println("json deserialize err: ", err)
		return
	}
}

func getNewMatchID() int {
	return matchNumCount - 1
}

func setNewUindex(uindex map[string]int, ip1 string, ip2 string) {
	uindex[ip1] = 0
	uindex[ip2] = 1
}

func setMyCondition(latestCondition clientCondition, oldCondition *clientCondition) {
	*oldCondition = latestCondition
}

func sendOpponentCondition(opponentCond clientCondition, myConn *websocket.Conn) {

	msgToClient := msgToClientJsonFormat{
		LatestBoard:       opponentCond.latestBoard,
		IsLosed:           opponentCond.isLosed,
		LatestMsgNum:      opponentCond.latestMsgNum,
		LatestAttackPoint: opponentCond.latestAttackPoint,
		OpponentAddr:      opponentCond.conn.RemoteAddr().String(),
	}

	json, err := json.Marshal(msgToClient)
	if err != nil {
		fmt.Println("json serialize error: ", err)
		return
	}

	fmt.Println("msgToC: ", string(json))
	if err := myConn.WriteMessage(1, []byte("battling\n"+string(json))); err != nil {
		fmt.Println("writeMessage err: ", err)
	}
}
