#!/usr/bin/env python3
import argparse
import requests
import sys

from azure.identity import DefaultAzureCredential
from azure.keyvault.secrets import SecretClient
from azure.data.tables import TableServiceClient
from azure.core.exceptions import ResourceNotFoundError

API_URL = "https://opensky-network.org/api/metadata/aircraft/icao/{}"

# -----------------------------
# Azure Key Vault Integration
# -----------------------------
def get_secret_from_keyvault(vault_url: str, secret_name: str):
    try:
        credential = DefaultAzureCredential()
        client = SecretClient(vault_url=vault_url, credential=credential)
        secret = client.get_secret(secret_name)
        return secret.value
    except Exception as ex:
        print(f"Warning: Could not retrieve secret '{secret_name}' from Key Vault: {ex}")
        return None

# -----------------------------
# Online Aircraft Lookup Logic
# -----------------------------
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

# ----------------------------------------------------------------------------
# Retrieve an aircraft entity from Azure Table Storage
# ----------------------------------------------------------------------------
def get_aircraft_by_icao24(connection_string: str, icao24: str):
    table_name = "Aircraft"
    partition_key = "Aircraft"

    service = TableServiceClient.from_connection_string(conn_str=connection_string)
    table = service.get_table_client(table_name)

    try:
        entity = table.get_entity(
            partition_key=partition_key,
            row_key=icao24.upper()
        )
        return {
            "PartitionKey": entity.get("PartitionKey"),
            "RowKey": entity.get("RowKey"),
            "IcaoAircraftType": entity.get("IcaoAircraftType"),
            "IcaoOperator": entity.get("IcaoOperator"),
            "Registration": entity.get("Registration"),
        }
    except ResourceNotFoundError:
        return None

# ----------------------------------------------------------------------------
# Upsert aircraft entity into Azure Table Storage
# ----------------------------------------------------------------------------
def upsert_aircraft(connection_string: str, icao24: str, registration: str, icao_type: str, operator: str):
    table_name = "Aircraft"
    partition_key = "Aircraft"

    service = TableServiceClient.from_connection_string(connection_string)
    table = service.get_table_client(table_name)

    entity = {
        "PartitionKey": partition_key,
        "RowKey": icao24.upper(),
        "Registration": registration,
        "IcaoAircraftType": icao_type,
        "IcaoOperator": operator,
    }

    table.upsert_entity(entity)
    print(f"Upserted aircraft {icao24.upper()} into table storage.")

# -----------------------------
# CLI Entry Point
# -----------------------------
def main():
    parser = argparse.ArgumentParser(
        description="FlightSpotter Aircraft Metadata Tool"
    )

    sub = parser.add_subparsers(dest="command", required=True)

    # ---- Online lookup ----
    online = sub.add_parser("online", help="Lookup aircraft metadata online via OpenSky")
    online.add_argument("icao24", help="ICAO24 hex code")

    # ---- Table lookup ----
    table = sub.add_parser("table", help="Lookup aircraft metadata from Azure Table Storage")
    table.add_argument("icao24", help="ICAO24 hex code")
    table.add_argument("--vault", default="https://planewatch-kv.vault.azure.net/")
    table.add_argument("--secret", default="Storage--ConnectionString")

    # ---- Upsert ----
    up = sub.add_parser("upsert", help="Upsert aircraft metadata into Azure Table Storage")
    up.add_argument("--vault", default="https://planewatch-kv.vault.azure.net/")
    up.add_argument("--secret", default="Storage--ConnectionString")
    up.add_argument("icao24", help="ICAO24 hex code")
    up.add_argument("--registration", required=True)
    up.add_argument("--type", required=True)
    up.add_argument("--operator", required=False)

    args = parser.parse_args()

    # --------------------------------------------------
    # Handle commands
    # --------------------------------------------------

    if args.command == "online":
        lookup(args.icao24)
        return

    # For table + upsert, retrieve connection string from Key Vault
    secret_value = get_secret_from_keyvault(args.vault, args.secret)
    if not secret_value:
        print("Could not retrieve storage connection string from Key Vault.")
        sys.exit(1)

    if args.command == "table":
        entity = get_aircraft_by_icao24(secret_value, args.icao24)
        if entity:
            print("Aircraft Entity:")
            for k, v in entity.items():
                print(f"  {k}: {v}")
        else:
            print("No aircraft entity found.")
        return

    if args.command == "upsert":
        upsert_aircraft(
            connection_string=secret_value,
            icao24=args.icao24,
            registration=args.registration,
            icao_type=args.type,
            operator=args.operator,
        )
        return


if __name__ == "__main__":
    main()
