package board

type Board struct {
	Cells                        []*Cell
	TotalNumberOfUsrDefusedCells int
	Height                       int
	Width                        int
	InitialMinesNum              int
	ClickedBomb                  bool
}

type Cell struct {
	IsOpenned    bool
	IsFlagged    bool
	WrittenValue int
	IsSafeBomb   bool
}

func (bd *Board) TryOpenCell(y int, x int, fromClick bool) {
	if bd.Cells[y*bd.Width+x].IsOpenned {
		return
	}

	if bd.Cells[y*bd.Width+x].IsFlagged {
		return
	}

	if bd.Cells[y*bd.Width+x].IsSafeBomb {
		return
	}

	if bd.Cells[y*bd.Width+x].WrittenValue == -1 {
		if fromClick {
			bd.ClickedBomb = true
		}
		return
	}

	if bd.Cells[y*bd.Width+x].WrittenValue > 0 {
		bd.Cells[y*bd.Width+x].IsOpenned = true
		if fromClick {
			bd.TotalNumberOfUsrDefusedCells++
		}
		bd.searchSafeMine(y, x)
		return
	}

	dx := []int{-1, 0, 1, -1, 1, -1, 0, 1}
	dy := []int{1, 1, 1, 0, 0, -1, -1, -1}

	type coordinate struct {
		y int
		x int
	}

	dfsStack := []coordinate{}
	dfsStack = append(dfsStack, coordinate{y: y, x: x})
	for {
		if len(dfsStack) == 0 {
			break
		}

		top := dfsStack[len(dfsStack)-1]
		dfsStack = dfsStack[:len(dfsStack)-1]
		ny := top.y
		nx := top.x
		if bd.Cells[ny*bd.Width+nx].IsOpenned {
			continue
		}
		if fromClick {
			bd.TotalNumberOfUsrDefusedCells++
		}
		bd.Cells[ny*bd.Width+nx].IsOpenned = true
		bd.searchSafeMine(ny, nx)

		for k := 0; k < 8; k++ {
			nny := ny + dy[k]
			nnx := nx + dx[k]

			if bd.isOutOfBoard(nny, nnx) {
				continue
			}

			if bd.Cells[nny*bd.Width+nnx].IsOpenned {
				continue
			}

			if bd.Cells[nny*bd.Width+nnx].WrittenValue == 0 {
				dfsStack = append(dfsStack, coordinate{y: nny, x: nnx})
			} else {
				if fromClick {
					bd.TotalNumberOfUsrDefusedCells++
				}
				bd.Cells[nny*bd.Width+nnx].IsOpenned = true
				bd.searchSafeMine(nny, nnx)
			}
		}
	}
}

func (bd *Board) TryFlagCell(y int, x int) {
	if bd.Cells[y*bd.Width+x].IsOpenned {
		return
	}
	if bd.Cells[y*bd.Width+x].IsSafeBomb {
		return
	}

	if bd.Cells[y*bd.Width+x].IsFlagged {
		bd.Cells[y*bd.Width+x].IsFlagged = false
	} else {
		bd.Cells[y*bd.Width+x].IsFlagged = true
	}
}

func (bd *Board) searchSafeMine(y int, x int) {
	for i := -1; i <= 1; i++ {
		for j := -1; j <= 1; j++ {
			ny := y + i
			nx := x + j
			if bd.isOutOfBoard(ny, nx) {
				continue
			}
			if bd.Cells[ny*bd.Width+nx].WrittenValue != -1 {
				continue
			}
			isSafe := true
			dx := []int{-1, 0, 1, -1, 1, -1, 0, 1}
			dy := []int{1, 1, 1, 0, 0, -1, -1, -1}
			for k := 0; k < 8; k++ {
				nny := ny + dy[k]
				nnx := nx + dx[k]
				if bd.isOutOfBoard(nny, nnx) {
					continue
				}
				isOpennedNumCell := bd.Cells[nny*bd.Width+nnx].IsOpenned && bd.Cells[nny*bd.Width+nnx].WrittenValue >= 0
				isUnopennedMine := !bd.Cells[nny*bd.Width+nnx].IsOpenned && bd.Cells[nny*bd.Width+nnx].WrittenValue == -1
				if !(isOpennedNumCell || isUnopennedMine) {
					isSafe = false
					break
				}
			}

			if isSafe {
				bd.Cells[ny*bd.Width+nx].IsSafeBomb = true
			}
		}
	}
}

func (bd *Board) isOutOfBoard(y int, x int) bool {
	return y < 0 || y >= bd.Height || x < 0 || x >= bd.Width
}
