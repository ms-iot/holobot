# Makefile
#
# Makefile for the Arduino AccelStepper project
#
# Author: Mike McCauley (mikem@airspayce.com)
# Copyright (C) 2010 Mike McCauley
# $Id: Makefile,v 1.5 2015/08/25 02:22:45 mikem Exp mikem $

PROJNAME = AccelStepper
VERSION_MAJOR = 1
VERSION_MINOR = 48

DISTFILE = $(PROJNAME)-$(VERSION_MAJOR).$(VERSION_MINOR).zip

all:	versioning doxygen dist upload

versioning:
	sed -i.bak -e 's/AccelStepper-.*\.zip/$(DISTFILE)/' AccelStepper.h

doxygen: 
	doxygen project.cfg

ci:
	(cd ..;ci -l `cat $(PROJNAME)/MANIFEST`)

dist:	
	(cd ..; zip $(PROJNAME)/$(DISTFILE) `cat $(PROJNAME)/MANIFEST`)

upload:
	rsync -avz $(DISTFILE) doc/ www.airspayce.com:public_html/mikem/arduino/$(PROJNAME)
