package battling

import (
	"encoding/json"
	"fmt"
	"sort"

	"github.com/gorilla/websocket"
)

var allMatches = make(map[string]*match)
var matchNumCount = 0

var MAX_INFLICTED_DAMAGE = 50

type msgFromClientJsonFormat struct {
	LatestBoard                  []int
	IsLosed                      bool
	LatestMsgNum                 int
	TotalNumberOfUsrDefusedCells int
	OpponentAddr                 string
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
	latestBoard               []int
	isLosed                   bool
	latestMsgNum              int
	attackPointToBeInflicted  int
	conn                      *websocket.Conn
	isReady                   bool
	totalDamageInflictedToOpp int
	totalNumOfOpenedCells     int
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

	joiningMatch := allMatches[getSortedAddrs(myIpAddr, c.OpponentAddr)]

	myUindex := joiningMatch.uindex[myIpAddr]

	// TODO: ダメージを一回のダメージ上限を作れるように。サーバー側をいじる.

	// update client's total num of opened cells to the latest one.
	joiningMatch.clients[myUindex].totalNumOfOpenedCells = c.TotalNumberOfUsrDefusedCells

	myCond := clientCondition{
		latestBoard:               c.LatestBoard,
		isLosed:                   c.IsLosed,
		latestMsgNum:              c.LatestMsgNum,
		attackPointToBeInflicted:  calculateDamegeToInflictToOpponent(joiningMatch.clients[myUindex].totalDamageInflictedToOpp, joiningMatch.clients[myUindex].totalNumOfOpenedCells),
		conn:                      conn,
		isReady:                   true,
		totalDamageInflictedToOpp: joiningMatch.clients[myUindex].totalDamageInflictedToOpp,
		totalNumOfOpenedCells:     joiningMatch.clients[myUindex].totalNumOfOpenedCells,
	}

	fmt.Println("addr: ", myIpAddr, " uindex: ", myUindex)

	setMyCondition(
		myCond,
		&joiningMatch.clients[myUindex],
	)

	if joiningMatch.clients[1-myUindex].isReady {
		sendOpponentCondition(joiningMatch.clients[1-myUindex], conn)
		updateOppCondWithDmgHeInflicted(getSortedAddrs(myIpAddr, c.OpponentAddr), 1-myUindex)

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
		LatestAttackPoint: opponentCond.attackPointToBeInflicted,
		OpponentAddr:      opponentCond.conn.RemoteAddr().String(),
	}

	// for debug ->
	tmpMsgToClient := msgToClient
	tmpMsgToClient.LatestBoard = []int{0}
	tmpJson, err := json.Marshal(tmpMsgToClient)
	if err != nil {
		fmt.Println("json serialize error: ", err)
		return
	}
	fmt.Println("msgToC: ", string(tmpJson))
	// <- for debug

	json, err := json.Marshal(msgToClient)
	if err != nil {
		fmt.Println("json serialize error: ", err)
		return
	}

	if err := myConn.WriteMessage(1, []byte("battling\n"+string(json))); err != nil {
		fmt.Println("writeMessage err: ", err)
	}
}

func calculateDamegeToInflictToOpponent(totalDamageInflictedToOpp int, totalNumOfOpenedCells int) int {
	damage := ((totalNumOfOpenedCells - totalDamageInflictedToOpp) / 10) * 10
	damage = Min4int(damage, MAX_INFLICTED_DAMAGE)
	return damage
}

func Min4int(a int, b int) int {
	if a < b {
		return a
	}
	return b
}

func updateOppCondWithDmgHeInflicted(matchIdx string, opponentUindex int) {
	opponentCondition := &allMatches[matchIdx].clients[opponentUindex]
	opponentCondition.totalDamageInflictedToOpp += opponentCondition.attackPointToBeInflicted
	opponentCondition.attackPointToBeInflicted = 0
}
