#!/bin/sh


#first we need to create the products
dotnet run --project src/PrintifyGenerator/

#then we need the metadata and pricing for the products
METADATA_UPDATER_STAGING_SHOP_NAME="Staging" dotnet run --project src/PrintifyGenerator.Metadata/

