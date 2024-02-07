package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"math/rand"
	"net/http"
	"slices"
	"strconv"
	"strings"
	"time"

	"yumarun/TM_cpu_server/pkg/board"
	"yumarun/TM_cpu_server/pkg/minesweeperSolver"

	"github.com/gorilla/websocket"
)

var level_interval = [3]time.Duration{4000, 1000, 400}

const BATTLING_INTERVAL = time.Duration(3)
const MAX_ADDED_LINES_NUM = 5
const INITIAL_OPEN_LINES_NUM = 3

var clientConnection *websocket.Conn

type cpuController struct {
	bd                     *board.Board
	totalNumOfDefucedCells int
	lv                     int
	oppAddr                string
	stt                    int // 0: battling, 1: won, 2: lost
	rating                 int
	cpuSolver              *minesweeperSolver.Solver
}

type jsonFromServer struct {
	LatestBoard       []int
	IsLosed           bool
	LatestMsgNum      int
	LatestAttackPoint int
	OpponentAddr      string
}

type jsonToServer struct {
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

func createBoard(iniBoard string, height int, width int, bombNum int) *board.Board {
	cellVals := strings.Split(iniBoard, ",")
	intCellVals := []int{}
	for _, v := range cellVals {
		intV, err := strconv.Atoi(v)
		if err != nil {
			panic(err)
		}
		intCellVals = append(intCellVals, intV)
	}

	cells := make([]*(board.Cell), len(cellVals))
	for i := 0; i < len(cellVals); i++ {
		cells[i] = &board.Cell{
			IsOpenned:    false,
			IsFlagged:    false,
			WrittenValue: intCellVals[i],
			IsSafeBomb:   false,
		}
	}

	return &board.Board{
		Cells:                        cells,
		TotalNumberOfUsrDefusedCells: 0,
		Height:                       height,
		Width:                        width,
		InitialMinesNum:              bombNum,
		ClickedBomb:                  false,
	}

}

func createCpu(lv int, oppAddr string, iniBoard string, height int, width int, bombNum int, sol *minesweeperSolver.Solver) *cpuController {
	fmt.Println("createCPU() called.")

	// lvからcpuのratingを取得
	endpoint := "https://tmapiserver.yumarun.net:8080/api/GetCpuRating/"
	body := []byte(strconv.Itoa(lv))
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
	ratingBytes, err := io.ReadAll(res.Body)
	fmt.Println(string(ratingBytes))
	rating, err := strconv.Atoi(string(ratingBytes))

	return &cpuController{
		bd:                     createBoard(iniBoard, height, width, bombNum),
		totalNumOfDefucedCells: 0,
		lv:                     lv,
		oppAddr:                oppAddr,
		stt:                    0,
		rating:                 rating,
		cpuSolver:              sol,
	}
}

func (c *cpuController) stop() {
	c.stt = -1
}

func (c *cpuController) startGame() {
	fmt.Println("startGame() called. Waiting 5 sec..")
	// x秒待った後
	time.Sleep(1 * time.Second)

	// 上3行を開ける
	for y := c.bd.Height - 1; y >= c.bd.Height-INITIAL_OPEN_LINES_NUM; y-- {
		for x := 0; x < c.bd.Width; x++ {
			c.bd.TryOpenCell(y, x, false)
		}
	}

	// 盤面更新用goroutine起動
	go updateCpuBoard(c.oppAddr)

	// 盤面送信用goroutine起動
	go sendInfoToServer(c.oppAddr)
}

/*
bool: This action lead cpu win.
*/
func (c *cpuController) solve() bool {
	var won bool
	c.bd, won = c.cpuSolver.Solve(c.bd)
	return won
}

func (c *cpuController) sendWinOrLose(win bool) {
	fmt.Println("sendWinOrLose() called.")
	if win {
		jts := jsonToServer{
			LatestBoard:                  []int{0},
			Lost:                         false,
			Won:                          true,
			LatestMsgNum:                 -1,
			TotalNumberOfUsrDefusedCells: -1,
			OpponentAddr:                 c.oppAddr,
			Name:                         ".cpu",
			Rating:                       c.rating,
			Subject:                      "a",
		}

		js, err := json.Marshal(jts)
		if err != nil {
			panic(err)
		}

		if err := clientConnection.WriteMessage(1, []byte("battling\n"+string(js))); err != nil {
			panic(err)
		}
	} else {
		jts := jsonToServer{
			LatestBoard:                  []int{0},
			Lost:                         true,
			Won:                          false,
			LatestMsgNum:                 -1,
			TotalNumberOfUsrDefusedCells: -1,
			OpponentAddr:                 c.oppAddr,
			Name:                         ".cpu",
			Rating:                       c.rating,
			Subject:                      "a",
		}

		js, err := json.Marshal(jts)
		if err != nil {
			panic(err)
		}

		if err := clientConnection.WriteMessage(1, []byte("battling\n"+string(js))); err != nil {
			panic(err)
		}
	}
}

func (c *cpuController) checkIfCpuWillLose(attackPoint int) bool {

	lose := false
	for i := c.bd.Height - 1; i >= c.bd.Height-attackPoint/10; i-- {
		for j := 0; j < c.bd.Width; j++ {
			if c.bd.Cells[i*c.bd.Width+j].WrittenValue == -1 && !c.bd.Cells[i*c.bd.Width+j].IsFlagged && !c.bd.Cells[i*c.bd.Width+j].IsSafeBomb {
				lose = true
			}
		}
	}

	return lose
}

func (c *cpuController) addLines(addedLinesNum int) {
	if addedLinesNum <= 0 {
		return
	}

	// addLInesNum行生成
	newLines := make([]int, c.bd.Width*addedLinesNum)
	for i := 0; i < c.bd.Width*addedLinesNum; i++ {
		newLines[i] = 0
	}
	bombPoss := []int{}
	for {
		if len(bombPoss) == (c.bd.InitialMinesNum/c.bd.Height)*addedLinesNum {
			break
		}

		idx := rand.Intn(len(newLines))
		if !slices.Contains(bombPoss, idx) {
			bombPoss = append(bombPoss, idx)
		}
	}
	for i := 0; i < len(bombPoss); i++ {
		newLines[i] = -1
	}

	// Cell配列を作成, 上addLInesNumを消して下に新しい奴に代入
	newCells := []*board.Cell{}
	for i := 0; i < addedLinesNum; i++ {
		for j := 0; j < c.bd.Width; j++ {
			newCell := &board.Cell{
				IsOpenned:    false,
				IsFlagged:    false,
				WrittenValue: newLines[i*c.bd.Width+j], // この段階で 0 or -1
				IsSafeBomb:   false,
			}
			newCells = append(newCells, newCell)
		}
	}
	for i := 0; i < c.bd.Height-addedLinesNum; i++ {
		for j := 0; j < c.bd.Width; j++ {
			newCell := &board.Cell{
				IsOpenned:    c.bd.Cells[i*c.bd.Width+j].IsOpenned,
				IsFlagged:    c.bd.Cells[i*c.bd.Width+j].IsFlagged,
				WrittenValue: c.bd.Cells[i*c.bd.Width+j].WrittenValue,
				IsSafeBomb:   c.bd.Cells[i*c.bd.Width+j].IsSafeBomb,
			}
			newCells = append(newCells, newCell)
		}

	}

	// &board.Board{}作成
	newBoard := &board.Board{
		Cells:                        newCells,
		TotalNumberOfUsrDefusedCells: c.bd.TotalNumberOfUsrDefusedCells,
		Height:                       c.bd.Height,
		Width:                        c.bd.Width,
		InitialMinesNum:              c.bd.InitialMinesNum,
		ClickedBomb:                  c.bd.ClickedBomb,
	}

	// &board.Board{}を更新(全cellのWritterValue更新 & 1行の中で上3の中に0があればそのセル開ける)
	for i := 0; i < c.bd.Height; i++ {
		for j := 0; j < c.bd.Width; j++ {
			if newBoard.Cells[i*c.bd.Width+j].WrittenValue >= 0 {
				dy := []int{1, 1, 1, 0, 0, -1, -1, -1}
				dx := []int{-1, 0, 1, -1, 1, -1, 0, 1}
				bombCnt := 0
				for k := 0; k < 8; k++ {
					ny := i + dy[k]
					nx := j + dx[k]
					if ny < 0 || ny >= c.bd.Height || nx < 0 || nx >= c.bd.Width {
						continue
					}
					if newBoard.Cells[ny*c.bd.Width+nx].WrittenValue == -1 {
						bombCnt++
					}
				}
				newBoard.Cells[i*c.bd.Width+j].WrittenValue = bombCnt
			}
		}
	}
	for j := 0; j < c.bd.Width; j++ {
		if newBoard.Cells[addedLinesNum*c.bd.Width+j].WrittenValue == 0 && newBoard.Cells[addedLinesNum*c.bd.Width+j].IsOpenned {
			for dx := -1; dx <= 1; dx++ {
				nx := j + dx
				if 0 <= nx && nx < c.bd.Width {
					newBoard.TryOpenCell(addedLinesNum-1, nx, false)
				}
			}
		}
	}

	// &Board.Boardをc.bdに反映
	c.bd = newBoard

	// solverのtmpFlagableCellをリセット
	c.cpuSolver.ResetTmpCheckableCell()
}

func (c *cpuController) sendinfo() {
	fmt.Println("sendinfo() called.")

	// json作成
	boardInts := []int{}
	for i := 0; i < c.bd.Height; i++ {
		for j := 0; j < c.bd.Width; j++ {
			if c.bd.Cells[i*c.bd.Width+j].IsSafeBomb {
				boardInts = append(boardInts, 10)
			} else if c.bd.Cells[i*c.bd.Width+j].IsFlagged {
				boardInts = append(boardInts, 9)
			} else if !c.bd.Cells[i*c.bd.Width+j].IsOpenned {
				boardInts = append(boardInts, -1)
			} else {
				boardInts = append(boardInts, c.bd.Cells[i*c.bd.Width+j].WrittenValue)
			}
		}
	}

	jts := jsonToServer{
		LatestBoard:                  boardInts,
		Lost:                         false,
		Won:                          false,
		LatestMsgNum:                 0,
		TotalNumberOfUsrDefusedCells: c.bd.TotalNumberOfUsrDefusedCells,
		OpponentAddr:                 c.oppAddr,
		Name:                         ".cpu",
		Rating:                       c.rating,
		Subject:                      "a",
	}

	js, err := json.Marshal(jts)
	if err != nil {
		panic(err)
	}

	// サーバーに送信
	if err := clientConnection.WriteMessage(1, []byte("battling\n"+string(js))); err != nil {
		panic(err)
	}

	fmt.Println("info: ", string(js))
}

func (c *cpuController) setNewRating(rating int) {
	endpoint := "https://tmapiserver.yumarun.net:8080/api/SetCpuRating/"
	body := []byte(strconv.Itoa(rating) + "\n" + strconv.Itoa(c.lv))
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
	resFromServer, err := io.ReadAll(res.Body)
	fmt.Println(string(resFromServer))
	if err != nil {
		panic(err)
	}
}

func updateCpuBoard(oppoAddr string) {
	fmt.Println("updateCpuBoard() called")
	t := time.NewTicker(level_interval[cpus[oppoAddr].lv] * time.Millisecond)
	for {
		select {
		case <-t.C:
			fmt.Println("update cpu board!")

			_, ok := cpus[oppoAddr]
			if !ok || cpus[oppoAddr].stt != 0 {
				log.Println("updateCpuBoard() finished.")

				return
			}

			w := cpus[oppoAddr].solve()
			if w {
				fmt.Println("CPU won!")
				cpus[oppoAddr].stt = 1
				cpus[oppoAddr].sendWinOrLose(true)
			}

		}
	}
}

func onServerMessageReceived(oppoAddr string, ap int) {
	time.Sleep(2 * time.Second)
	l := cpus[oppoAddr].checkIfCpuWillLose(ap)
	cpus[oppoAddr].addLines(ap / 10)
	if l {
		cpus[oppoAddr].stt = 2
		cpus[oppoAddr].sendWinOrLose(false)
	}
}

func sendInfoToServer(oppoAddr string) {
	fmt.Println("sendInfoToServer() called")

	t := time.NewTicker(BATTLING_INTERVAL * time.Second)
	for {
		select {
		case <-t.C:
			log.Println("send info to server!")
			_, ok := cpus[oppoAddr]

			if !ok || cpus[oppoAddr].stt != 0 {
				log.Println("sendInfoToServer() finished.")
				return
			}

			cpus[oppoAddr].sendinfo()
		}
	}
}

func deleteCpu(addr string) {
	delete(cpus, addr)
}

var cpus = make(map[string](*cpuController))

func main() {
	fmt.Println("cpu server start...")

	c, _, err := websocket.DefaultDialer.Dial("wss://tmserver.yumarun.net:8080/ws", nil)
	clientConnection = c
	if err != nil {
		panic(err)
	}

	fmt.Println("79")

	defer clientConnection.Close()
	defer c.Close()

	if err := c.WriteMessage(1, []byte("CpuConnectionStart")); err != nil {
		panic(err)
	}

	for {
		fmt.Println("85")

		_, msg, err := c.ReadMessage()
		if err != nil {
			panic(err)
		}
		fmt.Println("recv: ", string(msg))

		msgsp := strings.Split(string(msg), "\n")

		switch msgsp[0] {
		case "match!op:":
			fmt.Println("cpu matched!")
			lv, err := strconv.Atoi(msgsp[1])
			oppoAddr := msgsp[2]
			if err != nil {
				panic(err)
			}

			height, err := strconv.Atoi(msgsp[4])
			if err != nil {
				panic(err)
			}
			width, err := strconv.Atoi(msgsp[5])
			if err != nil {
				panic(err)
			}
			bombNum, err := strconv.Atoi(msgsp[6])
			if err != nil {
				panic(err)
			}
			cpuSolver := minesweeperSolver.NewSolver([2]int{-1, -1}, height, width)

			cpu := createCpu(lv, oppoAddr, msgsp[3], height, width, bombNum, cpuSolver)
			cpus[oppoAddr] = cpu
			cpus[oppoAddr].startGame()
		case "battling":

			var jfs jsonFromServer
			if err := json.Unmarshal([]byte(msgsp[1]), &jfs); err != nil {
				panic(err)
			}

			onServerMessageReceived(jfs.OpponentAddr, jfs.LatestAttackPoint)

		case "opponent disconnected. you win.":

		case "YouWon":
			fmt.Println("YouWon!")
			cpus[msgsp[1]].stop()

		case "YouLost":
			fmt.Println("YouLost!")
			cpus[msgsp[1]].stop()

		case "SendNewRating":
			fmt.Println("SendNewRating!")

			rating, err := strconv.Atoi(msgsp[1])
			if err != nil {
				panic(err)
			}
			cpus[msgsp[2]].setNewRating(rating)
			deleteCpu(msgsp[2])
		default:
			log.Println("default: ", msgsp[0])
		}

	}

}
