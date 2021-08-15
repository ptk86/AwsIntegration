terraform {
}

provider "aws" {
}

resource "aws_vpc" "kilinski-workshop" {
  cidr_block = "10.10.0.0/16"
  tags = {
    Name = "main"
  }
}