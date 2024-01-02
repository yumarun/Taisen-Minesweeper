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
	id int
	token string 
	name string
	rating int
}


var mysqlPassword string

// GET (token -> name, rating)
func getNameAndRating(w http.ResponseWriter, r *http.Request) {
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
	fmt.Fprintf(w, name + "\n" + strconv.Itoa(rating))
}



// GET (token, name -> fail or success)
func setRating(w http.ResponseWriter, r *http.Request) {
	body := make([]byte, r.ContentLength)
	r.Body.Read(body)
	args := strings.Split(string(body), " ")
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

func main() {
	fmt.Println("api server start!")
	
	f, err := os.Open("MYSQL_PASSWORD.txt")
	if err != nil {
		panic(err)
	}

	defer f.Close()

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
	
	rows, err := db.Query("SELECT * FROM users_test")
	var userResult []dbEntry
	for rows.Next() {
		user := dbEntry{}
		if err := rows.Scan(&user.id, &user.token, &user.name, &user.rating); err != nil {
			log.Fatal(err)
		}
		userResult = append(userResult, user)
	}
	for _, u := range(userResult) {
		fmt.Println("id: ", u.id, " token: ", u.token, " name: ", u.name, " rating: ", u.rating)
	}

	if err != nil {
		log.Fatal(err)
	}

	defer rows.Close()

	http.HandleFunc("/api/SetName/", setName)
	http.HandleFunc("/api/SetRating/", setRating)
	http.HandleFunc("/api/NameAndRating/", getNameAndRating)
	log.Fatal(http.ListenAndServe(":8080", nil))
	

	fmt.Println("fin")
}
