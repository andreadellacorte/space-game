#!/usr/bin/env bash

# This script builds the full project by running all other provided shell scripts in sequence

FOLDER='/Users/andrea/Documents/GitHub/space-game'

function n() {
  osascript -e "display notification \"$1\" with title \"SpatialOS\""
}

function b() { # Build Project
  start=`gdate +%s%N`
  $FOLDER/SpatialOS/scripts/build_project.sh $1
  end=`gdate +%s%N`
  build_time=$( echo "scale=2;($end - $start)/1000000000" | bc -l )
  
  if [ $# -gt 0 ]; then
    n "$(basename $1) Build Complete in $build_time seconds"
  else
    n "Project Build Complete in $build_time seconds"
  fi
}

alias s="n 'Workers Started' && $FOLDER/SpatialOS/scripts/run_server.sh" # Server Start
alias c="n 'Client Started' && $FOLDER/client/run.sh" # Client Start
alias ks="lsof -ti tcp:5301 | xargs kill -9 && n 'Local Build Stopped'" # Kill Server
alias kw="ps -ef | grep \"127.0.0.1 22000\" | grep -v grep | awk '{print \$2}' | xargs kill -9 && n 'Workers Killed'" # Kill Workers
alias kc="ps -ef | grep Client.exe | grep -v grep | awk '{print \$2}' | xargs kill -9 && n 'Client Killed'" # Kill Client
alias sd="ks && kc" # Shutdown
alias bsw="b $FOLDER/PlanetWorker && ks && s" # Build Workers & Start Server
alias brw="b $FOLDER/PlanetWorker && kw" # Build & Restart Workers
alias brc="b $FOLDER/client && kc && c" # Build & Restart Client
alias bs="b && ks && s" # Build Project & Start Server