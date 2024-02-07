package minesweeperSolver

import (
	"yumarun/TM_cpu_server/pkg/board"
)

/*

board:

10: IsSafeBomb
9: IsFlagged
8-0: WrittenValue
-1:
-2: unopenned

*/

var DX = []int{-1, 0, 1, -1, 1, -1, 0, 1}
var DY = []int{1, 1, 1, 0, 0, -1, -1, -1}

type Solver struct {
	tmpCheckableCell [2]int
	height           int
	width            int
}

func NewSolver(tmpCheckableCell [2]int, height int, width int) *Solver {
	return &Solver{
		tmpCheckableCell: tmpCheckableCell,
		height:           height,
		width:            width,
	}
}

func (s *Solver) ResetTmpCheckableCell() {
	s.tmpCheckableCell[0] = -1
	s.tmpCheckableCell[1] = -1
}

func (s *Solver) Solve(bd *board.Board) (*board.Board, bool) {

	openableCells, checkableCells := s.calculate(bd)
	if s.tmpCheckableCell[0] != -1 {
		checkableCells[s.tmpCheckableCell[0]*s.width+s.tmpCheckableCell[1]] = true
	}

	for y := s.height - 1; y >= 0; y-- {
		for x := 0; x < s.width; x++ {
			if openableCells[y*s.width+x] {
				newBoard := s.click(bd, 0, y, x)
				won := checkWon(*newBoard)
				return newBoard, won

			}
		}
	}

	for y := s.height - 1; y >= 0; y-- {
		for x := 0; x < s.width; x++ {
			if checkableCells[y*s.width+x] {
				if y == s.tmpCheckableCell[0] && x == s.tmpCheckableCell[1] {
					s.tmpCheckableCell[0] = -1
					s.tmpCheckableCell[1] = -1
					newBoard := s.click(bd, 1, y, x)
					return newBoard, false
				}
			}
		}
	}

	for y := s.height - 1; y >= 0; y-- {
		for x := 0; x < s.width; x++ {
			c := bd.Cells[y*s.width+x]
			if !c.IsOpenned && !c.IsSafeBomb && !c.IsFlagged {
				if c.WrittenValue == -1 {
					s.tmpCheckableCell[0] = y
					s.tmpCheckableCell[1] = x
				}

				newBoard := s.click(bd, 0, y, x)
				w := checkWon(*bd)
				return newBoard, w
			}
		}
	}

	return bd, true
}

func (s *Solver) calculate(bd *board.Board) ([]bool, []bool) {
	openableCells := make([]bool, s.height*s.width)
	checkableCells := make([]bool, s.height*s.width)

	for i := 0; i < s.height; i++ {
		for j := 0; j < s.width; j++ {
			openableCells[i*s.width+j] = false
		}
	}

	for y := s.height - 1; y >= 0; y-- {
		for x := 0; x < s.width; x++ {
			c := bd.Cells[y*s.width+x]
			if !c.IsOpenned {
				continue
			}

			if !(1 <= c.WrittenValue && c.WrittenValue <= 8) {
				continue
			}

			unopennedCells := []int{}
			flaggedCells := []int{}

			for k := 0; k < 8; k++ {
				ny := y + DY[k]
				nx := x + DX[k]
				if ny < 0 || ny >= s.height || nx < 0 || nx >= s.width {
					continue
				}
				if bd.Cells[ny*s.width+nx].IsFlagged || bd.Cells[ny*s.width+nx].IsSafeBomb {
					flaggedCells = append(flaggedCells, k)
				} else if !bd.Cells[ny*s.width+nx].IsOpenned {
					unopennedCells = append(unopennedCells, k)
				}

			}

			remainingBomsNum := c.WrittenValue - len(flaggedCells)
			if remainingBomsNum == 0 {
				for k := 0; k < len(unopennedCells); k++ {
					openableCells[(y+DY[unopennedCells[k]])*s.width+(x+DX[unopennedCells[k]])] = true
				}
			} else if remainingBomsNum == len(unopennedCells) {
				for k := 0; k < len(unopennedCells); k++ {
					checkableCells[(y+DY[unopennedCells[k]])*s.width+(x+DX[unopennedCells[k]])] = true
				}
			}
		}
	}

	return openableCells, checkableCells
}

func (s *Solver) click(bd *board.Board, lr int, y int, x int) *board.Board {
	if lr == 1 {
		bd.TryFlagCell(y, x)
	} else {
		bd.TryOpenCell(y, x, true)
	}
	return bd
}

func checkWon(bd board.Board) bool {

	opennedCellsNum := 0
	for i := 0; i < len(bd.Cells); i++ {
		if bd.Cells[i].IsOpenned || bd.Cells[i].IsFlagged || bd.Cells[i].IsSafeBomb {
			opennedCellsNum++
		}
	}

	return (opennedCellsNum == len(bd.Cells))
}
