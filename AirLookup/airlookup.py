#!/usr/bin/env python3
import argparse
import requests
import sys

API_URL = "https://opensky-network.org/api/metadata/aircraft/icao/{}"

def lookup(icao24: str):
    url = API_URL.format(icao24.lower())
    resp = requests.get(url, timeout=10)

    if resp.status_code == 404:
        print(f"No aircraft found for ICAO24: {icao24}")
        sys.exit(1)

    resp.raise_for_status()
    data = resp.json()

    print(f"ICAO24:        {data.get('icao24')}")
    print(f"Registration:  {data.get('registration')}")
    print(f"ICAO Type:     {data.get('typecode')}")
    print(f"Model:         {data.get('model')}")
    print(f"Manufacturer:  {data.get('manufacturericao')}")
    print(f"Operator:      {data.get('operator')}")
    print(f"Serial:        {data.get('serialnumber')}")

def main():
    parser = argparse.ArgumentParser(
        description="Lookup aircraft metadata from ICAO24 hex."
    )
    parser.add_argument("icao24", help="ICAO24 hex code (e.g., A4A3F2)")
    args = parser.parse_args()

    lookup(args.icao24)

if __name__ == "__main__":
    main()
