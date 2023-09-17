var dataStyle =
[
	{"selector": "node",
		"style": {
			"width" : "data(Size)",
			"height" : "data(Size)",
			"content" : "data(id)",
			"color" : "white",
			"background-color" : "data(backgroundcolor)",
			"border-color" : "data(bordercolor)",
			"border-width" : "2px",
			"background-opacity" : 0.8,
			"text-outline-width" : 1,
			"text-outline-color" : "#888",
			"pie-size" : "80%"
		}
	},
	{"selector": "edge",
		"style": {
			"curve-style" : "bezier",
			"width" : 4,
			"color" : "white",
			"label": "data(score)",
			"line-color" : "data(linecolor)",
			"opacity" : 0.5
		}
	},
	{"selector": ".e_yellow",
		"style": {
			"curve-style" : "bezier",
			"width" : 4,
			"color" : "white",
			"label": "data(comment)",
			"line-color" : "data(linecolor)",
			"opacity" : 0.5
		}
	}
]
;