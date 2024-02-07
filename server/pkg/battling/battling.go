package battling

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
	"sort"
	"strconv"

	//"strings"

	"github.com/gorilla/websocket"
)

var allMatches = make(map[string]*match)
var ipAddr_oppIpAddr = make(map[string]string)
var matchNumCount = 0

var MAX_INFLICTED_DAMAGE = 50

type msgFromClientJsonFormat struct {
	LatestBoard                  []int
	Lost                         bool
	Won                          bool
	LatestMsgNum                 int
	TotalNumberOfUsrDefusedCells int
	OpponentAddr                 string
	Name                         string
	Rating                       int
	Subject                      string
}

type msgToClientJsonFormat struct {
	LatestBoard       []int
	IsLosed           bool
	LatestMsgNum      int
	LatestAttackPoint int
	OpponentAddr      string
}

type match struct {
	matchID    int
	uindex     map[string]int
	clients    [2]clientCondition
	isFinished bool
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
	name                      string
	rating                    int
	subject                   string
}

func initMatch(ip1 string, ip2 string) {
	fmt.Println("initMatch() is called")

	// register map[ip1]ip2 and map[ip2]ip1
	ipAddr_oppIpAddr[ip1] = ip2
	ipAddr_oppIpAddr[ip2] = ip1

	matchNumCount++
	newMatchID := getNewMatchID()

	var uindex = make(map[string]int)
	setNewUindex(uindex, ip1, ip2)

	newMatch := match{
		matchID:    newMatchID,
		uindex:     uindex,
		clients:    [2]clientCondition{},
		isFinished: false,
	}

	newMatch.clients[0].rating = -1
	newMatch.clients[1].rating = -1

	allMatches[getSortedAddrs(ip1, ip2)] = &newMatch
}

func Process(rawMsg string, conn *websocket.Conn) {
	fmt.Println("battling.Process() called.")
	fmt.Println("msg: ", rawMsg, ", ip: ", conn.RemoteAddr().String())

	c := msgFromClientJsonFormat{}
	deserializeMessage(rawMsg, &c)

	myIpAddr := conn.RemoteAddr().String()

	if !isMatchRegistered(myIpAddr, c.OpponentAddr) {
		initMatch(myIpAddr, c.OpponentAddr)
	}

	joiningMatch := allMatches[getSortedAddrs(myIpAddr, c.OpponentAddr)]

	if c.Lost {
		if !joiningMatch.isFinished {
			sendButtleOutcomeToClients(ipAddr_oppIpAddr[myIpAddr], false)
			OnGameFin(ipAddr_oppIpAddr[myIpAddr], myIpAddr)
			joiningMatch.isFinished = true
			return
		}

	}

	if c.Won {
		if !joiningMatch.isFinished {
			sendButtleOutcomeToClients(myIpAddr, false)
			OnGameFin(myIpAddr, ipAddr_oppIpAddr[myIpAddr])
			joiningMatch.isFinished = true
			return
		}

	}

	myUindex := joiningMatch.uindex[myIpAddr]

	// update client's total num of opened cells to the latest one.
	joiningMatch.clients[myUindex].totalNumOfOpenedCells = c.TotalNumberOfUsrDefusedCells

	myCond := clientCondition{
		latestBoard:               c.LatestBoard,
		isLosed:                   c.Lost,
		latestMsgNum:              c.LatestMsgNum,
		attackPointToBeInflicted:  calculateDamegeToInflictToOpponent(joiningMatch.clients[myUindex].totalDamageInflictedToOpp, joiningMatch.clients[myUindex].totalNumOfOpenedCells),
		conn:                      conn,
		isReady:                   true,
		totalDamageInflictedToOpp: joiningMatch.clients[myUindex].totalDamageInflictedToOpp,
		totalNumOfOpenedCells:     joiningMatch.clients[myUindex].totalNumOfOpenedCells,
		name:                      c.Name,
		rating:                    c.Rating,
		subject:                   c.Subject,
	}

	setMyCondition(
		myCond,
		&joiningMatch.clients[myUindex],
	)

	if joiningMatch.clients[1-myUindex].isReady {
		sendOpponentCondition(joiningMatch.clients[1-myUindex], conn)
		updateOppCondWithDmgHeInflicted(getSortedAddrs(myIpAddr, c.OpponentAddr), 1-myUindex)

	}

}

func getSortedAddrs(addr1 string, addr2 string) string {
	addrs := []string{addr1, addr2}
	sort.Strings(addrs)
	return addrs[0] + addrs[1]
}

func isMatchRegistered(addr1 string, addr2 string) bool {
	// cpuチェック
	// withoutPort := strings.Split(addr1, ":")[0]
	// if withoutPort == "153.120.121.242" {
	// 	return true
	// }

	_, ok := allMatches[getSortedAddrs(addr1, addr2)]
	fmt.Println("isMatchRegsiterred() called. ok: " + fmt.Sprintf("%t", ok) + " ip1: " + addr1 + " ip2: " + addr2)
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

	rating := (*oldCondition).rating
	*oldCondition = latestCondition
	if rating != -1 { // InitMatchが呼ばれた後はratingを更新しない
		(*oldCondition).rating = rating
	}
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

	// fmt.Println("199 ", totalDamageInflictedToOpp, totalNumOfOpenedCells, damage)

	return damage
}

func Min4int(a int, b int) int {
	if a < b {
		return a
	}
	return b
}

func Max4int(a int, b int) int {
	if a > b {
		return a
	}
	return b
}

func updateOppCondWithDmgHeInflicted(matchIdx string, opponentUindex int) {
	opponentCondition := &allMatches[matchIdx].clients[opponentUindex]
	opponentCondition.totalDamageInflictedToOpp += opponentCondition.attackPointToBeInflicted
	opponentCondition.attackPointToBeInflicted = 0
}

func OnClientDisconnected(ipAddr string) {
	fmt.Println("OnClientDisconnected() called.")

	_, ok := ipAddr_oppIpAddr[ipAddr]
	if !ok {
		fmt.Println("Client[" + ipAddr + "] disconnected but game already closed.")
		return
	}

	sendButtleOutcomeToClients(ipAddr_oppIpAddr[ipAddr], true)
	OnGameFin(ipAddr_oppIpAddr[ipAddr], ipAddr)
}

// ipAddr_oppIpAddr, allmatchesから削除
func OnGameFin(winnerIpAddr string, loserIpAddr string) {

	fmt.Println("On game fin. winner: " + winnerIpAddr)

	winnerId := allMatches[getSortedAddrs(winnerIpAddr, loserIpAddr)].uindex[winnerIpAddr]
	loserId := 1 - winnerId
	winner := allMatches[getSortedAddrs(winnerIpAddr, loserIpAddr)].clients[winnerId]
	loser := allMatches[getSortedAddrs(winnerIpAddr, loserIpAddr)].clients[loserId]
	updateUserProfileOnGameFin(winner, loser)

	delete(ipAddr_oppIpAddr, winnerIpAddr)
	delete(ipAddr_oppIpAddr, loserIpAddr)

	if allMatches[getSortedAddrs(winnerIpAddr, loserIpAddr)].clients[0].name != ".cpu" {
		allMatches[getSortedAddrs(winnerIpAddr, loserIpAddr)].clients[0].conn.Close()

	}
	if allMatches[getSortedAddrs(winnerIpAddr, loserIpAddr)].clients[1].name != ".cpu" {
		allMatches[getSortedAddrs(winnerIpAddr, loserIpAddr)].clients[1].conn.Close()

	}

	delete(allMatches, getSortedAddrs(winnerIpAddr, loserIpAddr))
}

func sendButtleOutcomeToClients(winnerIpAddr string, OnDisconnected bool) {
	fmt.Println("sendButtleOutcomeToClients() called. winnerIpaddr: ", winnerIpAddr, ", ondisconnected: ", OnDisconnected)

	nowMatch := allMatches[getSortedAddrs(winnerIpAddr, ipAddr_oppIpAddr[winnerIpAddr])]
	winnerId := nowMatch.uindex[winnerIpAddr]
	loserId := 1 - winnerId

	if OnDisconnected {
		winnerAddr := nowMatch.clients[winnerId].conn.RemoteAddr().String()

		fmt.Println("on disc, winner ip: ", winnerAddr)

		if err := nowMatch.clients[winnerId].conn.WriteMessage(1, []byte("opponent disconnected. you win.")); err != nil {
			fmt.Println("send win message err: ", err)
		}

		nowMatch.clients[winnerId].conn.Close()
	} else {
		loserAddr := nowMatch.clients[loserId].conn.RemoteAddr().String()
		winnerAddr := nowMatch.clients[winnerId].conn.RemoteAddr().String()
		log.Println("loserAddr: ", loserAddr, ", winnerAddr: ", winnerAddr)

		

		if err := nowMatch.clients[winnerId].conn.WriteMessage(1, []byte("YouWon\n"+loserAddr)); err != nil {
			fmt.Println("send message err: ", err)
		}

		if err := nowMatch.clients[loserId].conn.WriteMessage(1, []byte("YouLost\n"+winnerAddr)); err != nil {
			fmt.Println("send message err: ", err)
		}

	}

}

func updateUserProfileOnGameFin(winner clientCondition, loser clientCondition) {
	// Calculate the winner's new rating
	winnersNewRating := getNewRating(winner.rating, loser.rating, true)

	// Update the winner's profile
	if winner.name != ".cpu" {
		setNewRating(winnersNewRating, winner.subject)
	}

	// Calculate the loser's new rating
	losersNewRating := getNewRating(winner.rating, loser.rating, false)

	// Update the winner's profile
	if loser.name != ".cpu" {
		setNewRating(losersNewRating, loser.subject)

	}

	// Update ratings on client sides.
	updateRatingOnClientSide(winner, winnersNewRating, loser.conn.RemoteAddr().String())
	updateRatingOnClientSide(loser, losersNewRating, winner.conn.RemoteAddr().String())

}

func updateRatingOnClientSide(client clientCondition, newRating int, opponentAddr string) {
	if err := client.conn.WriteMessage(1, []byte("SendNewRating\n"+strconv.Itoa(newRating)+"\n"+opponentAddr)); err != nil {
		fmt.Println("send message err: ", err)
	}
}

func getNewRating(oldWinnerRating int, oldLoserRating int, isCalcForWinner bool) int {
	k := 32

	diff := float64(k) * ((float64(oldLoserRating)-float64(oldWinnerRating))/800.0 + 0.5)
	if isCalcForWinner {
		fmt.Println("oldWinnerRating: ", oldWinnerRating)
		fmt.Println("winner: ", oldWinnerRating+int(diff))
		return oldWinnerRating + int(diff)
	} else {
		fmt.Println("oldLoserRating: ", oldLoserRating)
		fmt.Println("loser: ", oldLoserRating-int(diff))
		return oldLoserRating - int(diff)
	}
}

func setNewRating(newRating int, subject string) {
	endpoint := "https://tmapiserver.yumarun.net:8080/api/SetRating/"

	body := []byte(subject + " " + strconv.Itoa(newRating))
	fmt.Println()
	buf := bytes.NewBuffer(body)
	req, err := http.NewRequest("POST", endpoint, buf)
	if err != nil {
		panic(err)
	}

	req.Header.Add("Content-Type", "application/x-www-form-urlencoded")

	client := &http.Client{}
	res, err := client.Do(req)
	if err != nil {
		panic(err)
	}

	defer res.Body.Close()

	b, err := io.ReadAll(res.Body)
	fmt.Println(string(b))

}
