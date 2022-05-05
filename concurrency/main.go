package main

import (
	"encoding/json"
	"fmt"
	"io/ioutil"
	"log"
	"net/http"
	"sync"
	"time"
)

type response struct {
	Status bool   `json:"status"`
	Err    string `json:"err"`
}

type Configuration struct {
	Concurrency int      `json:"concurrency"`
	Urls        []string `json:"urls"`
	BackendURL  string   `json:"backendURL"`
}

var BackendURL string = ""

func Requests(wg *sync.WaitGroup, ch <-chan string) {
	for (len(ch)>0){
		for url := range ch {
			t := time.Now()
			r, err := http.Get(fmt.Sprintf(BackendURL, url))
			var info string
			if err != nil {
				info = "internal error"
			}
			res := response{}
			defer r.Body.Close()
			json.NewDecoder(r.Body).Decode(&res)
			if res.Status {
				info = "Success"
			} else {
				info = "Failed"
				fmt.Println(res.Err)
			}
			duration := time.Since(t)
			fmt.Printf("该次url: %s 请求用时%v,%s\n", url, duration, info)
		}
	}
	wg.Done()
}

func main() {
	content, err := ioutil.ReadFile("config.json")
	var config Configuration
	json.Unmarshal(content, &config)
	if err != nil {
		log.Fatalln(err)
	}
	BackendURL = config.BackendURL
	var wg sync.WaitGroup
	l := len(config.Urls)
	ch := make(chan string, l)
	for i := 0; i < l; i++ {
		ch <- config.Urls[i]
	}
	for index := 0; index < config.Concurrency; index++ {
		wg.Add(1)
		if index > l {
			go Requests(&wg, ch)
		} else {
			go Requests(&wg, ch)
		}
	}
	wg.Wait()
	close(ch)
}
