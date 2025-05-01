#!/bin/bash

# Test script for SampleApi with local key vault
echo "Testing SampleApi with local key vault..."

# Test the /weatherforecast endpoint
echo -e "\nTesting /weatherforecast endpoint:"
curl --max-time 5 http://localhost:5000/weatherforecast

# Test the /config endpoint
echo -e "\n\nTesting /config endpoint:"
curl --max-time 5 http://localhost:5000/config | jq .

echo -e "\nDone testing SampleApi."
