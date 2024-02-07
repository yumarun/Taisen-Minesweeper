package main

import (
	"database/sql"
	"fmt"
	"log"
	"net/http"
	"os"
	"strconv"
	"strings"

	_ "github.com/go-sql-driver/mysql"
)

type dbEntry struct {
	id     int
	token  string
	name   string
	rating int
}

type dbCpuEntry struct {
	id     int
	level  int
	rating int
}

var mysqlPassword string

// var allowOrigin = "https://taisen-minesweeper-webglver.web.app"
var allowOrigin = "https://taisen-minesweeper-test.web.app"
var selfOrigin = "https://tmapiserver.yumarun.net"

// GET (token -> name, rating)
func getNameAndRating(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Access-Control-Allow-Origin", allowOrigin)
	w.Header().Set("Access-Control-Allow-Methods", "GET,PUT,POST,DELETE,UPDATE,OPTIONS")
	w.Header().Set("Access-Control-Allow-Credentials", "true")

	name := "x"
	rating := -1

	body := make([]byte, r.ContentLength)
	r.Body.Read(body)

	token := string(body)
	log.Println("request body: ", token)
	db, err := sql.Open("mysql", "root:"+mysqlPassword+"@tcp(localhost:3306)/TM")
	if err != nil {
		panic(err)
	}
	defer db.Close()

	rows, err := db.Query("SELECT * FROM users_test where token = ?", token)
	var usersResult []dbEntry
	for rows.Next() {
		user := dbEntry{}
		if err := rows.Scan(&user.id, &user.token, &user.name, &user.rating); err != nil {
			panic(err)
		}
		usersResult = append(usersResult, user)
	}
	if len(usersResult) >= 1 {
		name = usersResult[0].name
		rating = usersResult[0].rating
	}
	fmt.Fprintf(w, name+"\n"+strconv.Itoa(rating))
}

// GET (token, rating -> fail or success)
func setRating(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Access-Control-Allow-Origin", allowOrigin)
	w.Header().Set("Access-Control-Allow-Methods", "GET,PUT,POST,DELETE,UPDATE,OPTIONS")
	w.Header().Set("Access-Control-Allow-Credentials", "true")

	body := make([]byte, r.ContentLength)
	r.Body.Read(body)
	args := strings.Split(string(body), " ")
	fmt.Println("len(args): " + strconv.Itoa(len(args)) + ", args[1]: " + args[1])
	rating, err := strconv.Atoi(args[1])
	if err != nil {
		panic(err)
	}
	log.Println("request body: ", args[0], " , ", args[1])
	db, err := sql.Open("mysql", "root:"+mysqlPassword+"@tcp(localhost:3306)/TM")
	if err != nil {
		panic(err)
	}
	defer db.Close()

	upd, err := db.Prepare("UPDATE users_test set rating = ? where token = ?")
	if err != nil {
		log.Fatal(err)
	}

	upd.Exec(rating, args[0])

	fmt.Fprintf(w, "fin")
}

// GET (token, name -> fail or success)
func setName(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Access-Control-Allow-Origin", allowOrigin)
	w.Header().Set("Access-Control-Allow-Methods", "GET,PUT,POST,DELETE,UPDATE,OPTIONS")
	w.Header().Set("Access-Control-Allow-Credentials", "true")

	body := make([]byte, r.ContentLength)
	r.Body.Read(body)
	args := strings.Split(string(body), " ")
	log.Println("request body: ", args[0], " , ", args[1])
	db, err := sql.Open("mysql", "root:"+mysqlPassword+"@tcp(localhost:3306)/TM")
	if err != nil {
		panic(err)
	}
	defer db.Close()

	upd, err := db.Prepare("UPDATE users_test set name = ? where token = ?")
	if err != nil {
		log.Fatal(err)
	}

	upd.Exec(args[1], args[0])

	fmt.Fprintf(w, "fin")
}

// POST (token, name)
func registerAccount(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Access-Control-Allow-Origin", allowOrigin)
	w.Header().Set("Access-Control-Allow-Methods", "GET,PUT,POST,DELETE,UPDATE,OPTIONS")
	w.Header().Set("Access-Control-Allow-Credentials", "true")

	body := make([]byte, r.ContentLength)
	r.Body.Read(body)
	args := strings.Split(string(body), "%20")
	fmt.Println("args length: ", len(args), " 0: ", args[0])
	db, err := sql.Open("mysql", "root:"+mysqlPassword+"@tcp(localhost:3306)/TM")
	if err != nil {
		panic(err)
	}
	defer db.Close()

	ins, err := db.Prepare("INSERT INTO users_test (token,name,rating) VALUES(?,?,?)")
	if err != nil {
		panic(err)
	}
	ins.Exec(args[0], args[1], 1500)

	fmt.Fprintf(w, "fin")
}

// POST (name)
func checkDeplicateName(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Access-Control-Allow-Origin", allowOrigin)
	w.Header().Set("Access-Control-Allow-Methods", "GET,PUT,POST,DELETE,UPDATE,OPTIONS")
	w.Header().Set("Access-Control-Allow-Credentials", "true")

	name := make([]byte, r.ContentLength)
	r.Body.Read(name)
	db, err := sql.Open("mysql", "root:"+mysqlPassword+"@tcp(localhost:3306)/TM")
	if err != nil {
		panic(err)
	}
	defer db.Close()

	rows, err := db.Query("SELECT * FROM users_test where name = ?", name)
	if err != nil {
		panic(err)
	}
	count := 0
	for rows.Next() {
		user := dbEntry{}
		if err := rows.Scan(&user.id, &user.token, &user.name, &user.rating); err != nil {
			log.Fatal(err)
		}
		count = count + 1
	}
	defer rows.Close()

	if count == 0 {
		fmt.Fprintf(w, "ok")
	} else {
		fmt.Fprintf(w, "bad")
	}

}

// POST(lv)
func getCpuRating(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Access-Control-Allow-Origin", "*")
	w.Header().Set("Access-Control-Allow-Methods", "GET,PUT,POST,DELETE,UPDATE,OPTIONS")
	w.Header().Set("Access-Control-Allow-Credentials", "true")

	lv := make([]byte, r.ContentLength)
	r.Body.Read(lv)
	db, err := sql.Open("mysql", "root:"+mysqlPassword+"@tcp(localhost:3306)/TM")
	if err != nil {
		panic(err)
	}
	defer db.Close()

	lvInt, err := strconv.Atoi(string(lv))
	if err != nil {
		panic(err)
	}

	rows, err := db.Query("SELECT * FROM cpu_ratings where level = ?", lvInt)
	if err != nil {
		panic(err)
	}
	var cpuResult []dbCpuEntry
	for rows.Next() {
		cpu := dbCpuEntry{}
		if err := rows.Scan(&cpu.id, &cpu.level, &cpu.rating); err != nil {
			panic(err)
		}
		cpuResult = append(cpuResult, cpu)
	}

	var rating int
	if len(cpuResult) >= 1 {
		rating = cpuResult[0].rating
	}
	fmt.Fprintf(w, strconv.Itoa(rating))
}

// POST(rating,lv)
func setCpuRating(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Access-Control-Allow-Origin", "*")
	w.Header().Set("Access-Control-Allow-Methods", "GET,PUT,POST,DELETE,UPDATE,OPTIONS")
	w.Header().Set("Access-Control-Allow-Credentials", "true")

	body := make([]byte, r.ContentLength)
	r.Body.Read(body)
	bodySplitted := strings.Split(string(body), "\n")
	rating, err := strconv.Atoi(bodySplitted[0])
	if err != nil {
		panic(err)
	}
	lv, err := strconv.Atoi(bodySplitted[1])
	if err != nil {
		panic(err)
	}

	db, err := sql.Open("mysql", "root:"+mysqlPassword+"@tcp(localhost:3306)/TM")
	if err != nil {
		panic(err)
	}
	defer db.Close()

	upd, err := db.Prepare("UPDATE cpu_ratings set rating = ? where level = ?")
	if err != nil {
		log.Fatal(err)
	}

	upd.Exec(rating, lv)

	fmt.Fprintf(w, "rating set fin")
}

func main() {
	fmt.Println("api server start!")

	f, err := os.Open("/home/ubuntu/TM_api/api_server/MYSQL_PASSWORD.txt")
	if err != nil {
		panic(err)
	}

	defer f.Close()
	fmt.Println("179")
	buf := make([]byte, 64)
	n, err := f.Read(buf)
	if err != nil {
		panic(err)
	}

	mysqlPassword = string(buf[:n-1])

	db, err := sql.Open("mysql", "root:"+mysqlPassword+"@tcp(localhost:3306)/TM")
	if err != nil {
		panic(err)
	}

	defer db.Close()
	fmt.Println("194")

	rows, err := db.Query("SELECT * FROM users_test")
	if err != nil {
		log.Fatal(err)
	}

	defer rows.Close()
	var userResult []dbEntry
	fmt.Println("203")

	for rows.Next() {
		user := dbEntry{}
		if err := rows.Scan(&user.id, &user.token, &user.name, &user.rating); err != nil {
			log.Fatal(err)
		}
		userResult = append(userResult, user)
	}
	for _, u := range userResult {
		fmt.Println("id: ", u.id, " token: ", u.token[0:7], " name: ", u.name, " rating: ", u.rating)
	}

	http.HandleFunc("/api/SetName/", setName)
	http.HandleFunc("/api/SetRating/", setRating)
	http.HandleFunc("/api/NameAndRating/", getNameAndRating)
	http.HandleFunc("/api/Register/", registerAccount)
	http.HandleFunc("/api/CheckDeplicateName/", checkDeplicateName)
	http.HandleFunc("/api/GetCpuRating/", getCpuRating)
	http.HandleFunc("/api/SetCpuRating/", setCpuRating)

	log.Fatal(http.ListenAndServeTLS(":8080", "/etc/letsencrypt/archive/tmapiserver.yumarun.net/fullchain1.pem", "/etc/letsencrypt/archive/tmapiserver.yumarun.net/privkey1.pem", nil))

	fmt.Println("fin")
}
