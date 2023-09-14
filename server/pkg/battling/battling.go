package battling

import (
	"sort"

	"github.com/gorilla/websocket"
)

type match struct {
	matchID int
	players [2]*gamePlayer
}

type gamePlayer struct {
	info          userInfo
	latestMessage clientMessage
}

func newGamePlayer(info userInfo) *gamePlayer {
	nGamePlayer := &gamePlayer{
		info:          info,
		latestMessage: clientMessage{},
	}

	return nGamePlayer
}

func newMatch(matchID int, player1 userInfo, player2 userInfo) *match {

	var players [2]*gamePlayer
	players[0] = newGamePlayer(player1)
	players[1] = newGamePlayer(player2)

	nMatch := &match{
		matchID: matchID,
		players: players,
	}
	return nMatch
}

var allMatches = make(map[string]*match)

type userInfo struct {
	uid    int
	ipAddr string
	conn   *websocket.Conn
}

type clientMessage struct {
	board  boardState
	msgNum int
	// あと与えるダメージとか攻撃の有無とか。
}

type boardState struct {
}

func initMatch(uid1 int, ipAddr1 string, conn1 *websocket.Conn, uid2 int, ipAddr2 string, conn2 *websocket.Conn) {

	matchNumber := len(allMatches) + 1
	nMatch := newMatch(
		matchNumber,
		userInfo{uid: uid1, ipAddr: ipAddr1, conn: conn1},
		userInfo{uid: uid2, ipAddr: ipAddr2, conn: conn2},
	)
	allMatches[getSortedAddrs(ipAddr1, ipAddr2)] = nMatch
}

func Process(matchID int, uid int, myAddr string, opponentAddr string, conn *websocket.Conn) {

	// もしまだ決まっていない試合ならinitMatchを呼ぶ
	if matchID == -1 {
		if isMatchNotRegistered(myAddr, opponentAddr) {
			initMatch()
		}
	}
}

func getSortedAddrs(addr1 string, addr2 string) string {
	addrs := []string{addr1, addr2}
	sort.Strings(addrs)
	return addrs[0] + addrs[1]
}

func isMatchNotRegistered(addr1 string, addr2 string) bool {
	_, ok := allMatches[getSortedAddrs(addr1, addr2)]
	return ok
}
