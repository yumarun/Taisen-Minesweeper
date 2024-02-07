package matching

import (
	"fmt"
	"math/rand"
	"strconv"
	"time"

	"github.com/gorilla/websocket"
	"golang.org/x/exp/slices"
)

type userInfo struct {
	uid    int
	ipAddr string
	conn   *websocket.Conn
}

var MAX_WAITING_DURATION time.Duration = 10 // sec
var users = make(map[string]userInfo)
var beInPool = make(map[string]bool)
var usrNumInPool = 0
var cpu userInfo

const (
	BOARD_WIDTH      = 15
	BOARD_HEIGHT     = 15
	INITIAL_MINE_NUM = 45
)

func waitCPU(addr string) {

	// Waiting xxx sec...
	time.Sleep(MAX_WAITING_DURATION * time.Second)

	// If usr is in pool after xxx sec, cancel match and start vs. cpu battle.
	if beInPool[addr] {
		fmt.Println(MAX_WAITING_DURATION, " sec elapsed and no one came matching pool.")
		usr := users[addr]

		cpuGameStart(usr)
	}
}

// Call this function on starting server.
func RegisterCPU(conn *websocket.Conn) {
	cpu = userInfo{uid: -1, ipAddr: conn.RemoteAddr().String(), conn: conn}
}

func cpuGameStart(usr userInfo) {
	fmt.Println("CPU game start!")
	usrNumInPool -= 1
	delete(beInPool, usr.ipAddr)
	if err := usr.conn.WriteMessage(1, []byte("match!op:\n"+cpu.ipAddr)); err != nil {
		fmt.Println("ok match err: ", err)
	}

	newCpuBoard := createInitialBoard()
	cpuLevel := rand.Intn(3)
	if err := cpu.conn.WriteMessage(1, []byte("match!op:\n"+strconv.Itoa(cpuLevel)+"\n"+usr.ipAddr+"\n"+newCpuBoard+"\n"+strconv.Itoa(BOARD_HEIGHT)+"\n"+strconv.Itoa(BOARD_WIDTH)+"\n"+strconv.Itoa(INITIAL_MINE_NUM))); err != nil {
		fmt.Println("ok match err: ", err)
	}
}

func Process(uid int, ipAddr string, conn *websocket.Conn) {
	if !isUserRegistered(ipAddr) {
		registerUser(ipAddr, userInfo{uid: uid, ipAddr: ipAddr, conn: conn})

		usr := users[ipAddr]
		addUserToPool(usr)
		go waitCPU(ipAddr)
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

func createInitialBoard() string {

	// board作成
	b := []int{}
	for i := 0; i < BOARD_HEIGHT*BOARD_WIDTH; i++ {
		b = append(b, 0)
	}

	// mineのポジションをINITIAL_MINE_NUM個保存
	boardPos := []int{}
	for {
		if len(boardPos) == INITIAL_MINE_NUM {
			break
		}
		p := rand.Intn(BOARD_HEIGHT * BOARD_WIDTH)
		if !slices.Contains(boardPos, p) {
			boardPos = append(boardPos, p)
		}
	}

	for i := 0; i < len(boardPos); i++ {
		b[boardPos[i]] = -1
	}

	// retを更新
	dx := [8]int{-1, 0, 1, -1, 1, -1, 0, 1}
	dy := [8]int{-1, -1, -1, 0, 0, 1, 1, 1}
	for i := 0; i < BOARD_HEIGHT; i++ {
		for j := 0; j < BOARD_WIDTH; j++ {
			if b[i*BOARD_WIDTH+j] != -1 {
				cnt := 0
				for k := 0; k < 8; k++ {
					y := i + dy[k]
					x := j + dx[k]
					if y < 0 || BOARD_HEIGHT <= y || x < 0 || BOARD_WIDTH <= x {
						continue
					}
					if b[y*BOARD_WIDTH+x] == -1 {
						cnt++
					}
				}
				b[i*BOARD_WIDTH+j] = cnt
			}
		}
	}

	// bをstringに変換
	ret := ""
	for i := 0; i < BOARD_HEIGHT; i++ {
		for j := 0; j < BOARD_WIDTH; j++ {
			ret += strconv.Itoa(b[i*BOARD_WIDTH+j])
			if i*BOARD_WIDTH+j != BOARD_HEIGHT*BOARD_WIDTH-1 {
				ret += ","
			}
		}
	}

	fmt.Println("board: ", ret)
	return ret
}
