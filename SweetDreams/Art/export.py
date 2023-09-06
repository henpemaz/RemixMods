from PyQt5 import QtCore
from PyQt5.QtWidgets import QFileDialog, QProgressDialog
import os
import json
from krita import *

k = Krita.instance()

q = QFileDialog()
q.setWindowTitle("The folder")
q.setFileMode(QFileDialog.Directory)
q.setOption(QFileDialog.ShowDirsOnly, True)
q.exec_()
filesdir = q.selectedFiles()[0].rstrip("/")

dreams = {
	"White": {
		"sleepingSprite": "sleep - 2 - white",
		"fallback": None,
		"dreams": {}
	},
	"Yellow": {
		"sleepingSprite": "sleep - 2 - yellow",
		"fallback": None,
		"dreams": {}
	},
	"Red":{
		"sleepingSprite": "sleep - 2 - red",
		"fallback": None,
		"dreams": {}
	}
}

slugnames = {"white":"White", "monk":"Yellow","hunter":"Red"}

lizardnames = {
	"axo":"Salamander",
	"blacklizard":"BlackLizard",
	"bluelizard":"BlueLizard",
	"cyanlizard":"CyanLizard",
	"greenlizard":"GreenLizard",
	"pinklizard":"PinkLizard",
	"redlizard":"RedLizard",
	"whitelizard":"WhiteLizard",
	"yellowlizard":"YelloLlizard",
	"orangelizard":"YelloLlizard"
}

songnames = {
	"axo":"sd_Salamander",
	"blacklizard":"sd_Black_Lizard",
	"bluelizard":"sd_Blue_Lizard",
	"cyanlizard":"sd_Cyan_Lizard",
	"greenlizard":"sd_Green_Lizard",
	"pinklizard":"sd_Pink_Lizard",
	"redlizard":"sd_Red_Lizard",
	"whitelizard":"sd_White_Lizard",
	"yellowlizard":"sd_Yellow_Lizard",
	"orangelizard":"sd_Yellow_Lizard"
}


k.setBatchmode(True)
for file in os.listdir(filesdir):
	if not file.endswith(".psd"):
		continue

	filename = file.split(".")[0]
	slug = filename.split("_")[0]
	lizor = filename.split("_")[1]
	dreams[slugnames[slug]]["dreams"][lizardnames[lizor]] = {
	"song": songnames[lizor],
	"layers": {}
	}

	istop = False

	doc = k.openDocument(filesdir + "/" +file)
	layers = doc.topLevelNodes()
	for layer in layers:
		layername = layer.name()
		if layername.endswith("[img]") or layername.endswith("[dpt]") or layername == "Background":
			if layername.lower().startswith(dreams[slugnames[slug]]["sleepingSprite"]) and layername.endswith("[img]"):
				print("found slug layer:" + layername)
				istop = True
			continue
		print("exporting layer: " + filename +" "+layername)
		print(istop)
		bounds = layer.bounds()
		exportParameters = InfoObject()
		exportParameters.setProperty("alpha", True)
		exportParameters.setProperty("compression", 6)
		exportParameters.setProperty("indexed", False)
		layer.save(filesdir + "/" +filename +" "+layername+".png",1,1,exportParameters,bounds)

		x,y,w,h = bounds.getRect()
		layerdef = {
			"sprite": filename +" "+layername,
			"onTop": istop,
			"pos": [x - 277,1080 - y - h - 156],
			"slugDepthOffset": -0.1 if istop else 0.1,
			"shader": "basic"
		  }
		dreams[slugnames[slug]]["dreams"][lizardnames[lizor]]["layers"]["top" if istop else "bottom"] = layerdef

json_object = json.dumps(dreams, indent=2)

with open(filesdir + "/dreams.json", "w") as outfile:
	outfile.write(json_object)
k.setBatchmode(False)