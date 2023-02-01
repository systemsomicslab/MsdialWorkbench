////////////////////////////////////////////////////////////////////////////////
//MZ-Intensity viewer
////////////////////////////////////////////////////////////////////////////////
function makeGraph(arrayData, strIdTag, numMinX, numInterval) {

	var targetPlot = $.jqplot(strIdTag, [ arrayData ], {
		seriesDefaults : {
			renderer : $.jqplot.BarRenderer,
			rendererOptions : {
				barPadding : -4,
				barMargin : 10,
				barWidth : 4,
				shadowOffset : 0,
				shadowDepth : 0,
				shadowAlpha : 0
			},
			pointLabels : {
				show : true,
				location : 'n',
				xpadding : 0,
				formatString : "%d"
			},
			showMarker : false,
			neighborThreshold : 0,
		},
		series : [ {
			label : 'intensity'
		} ],
		axes : {
			xaxis : {
				renderer : $.jqplot.DataAxisRenderer,
				labelRenderer: $.jqplot.CanvasAxisLabelRenderer,
				label : "m/z",
				min : numMinX,
				tickInterval : numInterval

			},
			yaxis : {
				renderer : $.jqplot.DataAxisRenderer,
				labelRenderer: $.jqplot.CanvasAxisLabelRenderer,
				label : 'Intensity',
				min : 0,
				tickOptions : {
					formatString : "%d"
				}
			}
		},
		highlighter : {
			show : true,
			showMarker : true,
			tooltipLocation : 'n',
			tooltipOffset : -7,
			tooltipAxes : 'xy',
			yvalues : 3,
			formatString : "%.3f , %d"
		},
		cursor : {
			show : true,
			zoom : true,
			showTooltip : true,
			looseZoom : false,
			constrainZoomTo : 'none'
		},
		axesDefaults : {
			tickOptions : {
				formatString : "%.2f"
			},
			autoscale : true,
			useSeriesColor : true
		}
	});
	targetPlot.replot();
}

function makeGraph2(arrayData, labelData, strIdTag, numMinX, numInterval) {

	var targetPlot = $.jqplot(strIdTag, [ arrayData ], {

		seriesDefaults : {
			renderer : $.jqplot.BarRenderer,
			rendererOptions : {
				barPadding : -4,
				barMargin : 10,
				barWidth : 4,
				shadowOffset : 0,
				shadowDepth : 0,
				shadowAlpha : 0
			},
			pointLabels : {
				show : true,
				location : 'n',
				labels: labelData,
				xpadding : 0,
				edgeTolerance: 16,
				formatString : "%d"
			},
			showMarker : false,
			neighborThreshold : 0,
		},
		grid: {
			drawGridLines: true,
			gridLineColor: '#ececec',
			background: '#fcfdfd',
			borderColor: '#9ad0f5',
			borderWidth: 2,
			shadow: false
		},
		series : [ {
			label : 'intensity',
			color : '#9ad0f5'
		} ],
		axes : {
			xaxis : {
				renderer : $.jqplot.DataAxisRenderer,
				labelRenderer: $.jqplot.CanvasAxisLabelRenderer,
				label : "m/z",
				min : numMinX,
				tickInterval : numInterval

			},
			yaxis : {
				renderer : $.jqplot.DataAxisRenderer,
				labelRenderer: $.jqplot.CanvasAxisLabelRenderer,
				label : 'Intensity',
				min : 0,
				tickOptions : {
					formatString : "%d"
				}
			}
		},
		highlighter : {
			show : true,
			showMarker : true,
			tooltipLocation : 'n',
			tooltipOffset : -7,
			tooltipAxes : 'xy',
			yvalues : 3,
			formatString : "%.3f , %d"
		},
		cursor : {
			show : true,
			zoom : true,
			showTooltip : true,
			looseZoom: true,
			constrainOutsideZoom : true,
			showTooltipOutsideZoom : false,
			constrainZoomTo : 'none'
		},
		axesDefaults : {
			tickOptions : {
				formatString : "%.2f"
			},
			autoscale : true,
			useSeriesColor : true
		}
	});
	targetPlot.replot();
}
////////////////////////////////////////////////////////////////////////////////
//Show Score
////////////////////////////////////////////////////////////////////////////////
function showScore(score, mX, mY){
	var html_dialog = '<div id="edgeScore-dialog">' + '<p>EdgeScore : ' + score + '</p></div>';
	jQuery(html_dialog).dialog({
		minWidth : 50,
		position: {my: ""+ mX +" " + mY, at:"center top", of: window },
		buttons : {
			"Close" : function() {
				$(this).dialog("close");
			}
		},
		close : function() {
			$(this).remove();
		}
	})
}

////////////////////////////////////////////////////////////////////////////////
//Show Dialog
////////////////////////////////////////////////////////////////////////////////
function showDialog(id,numX,numY) {
	var html_dialog = '<div id="node-dialog'
			+ id
			+ '" title="ID '
			+ id
			+ '"><table class="tableDetail"><tbody><tr><td class="areaDetailTitle"><b>ID</b></td><td class="areaDetailCells"><span id="labelID'
			+ id
			+ '"></span></td><td class="areaDetailSmiles" rowspan="3"><canvas id="barchart'
			+ id
			+ '"></canvas></td></tr><tr><td class="areaDetailTitle"><b>Ontology</b></td><td class="areaDetailCells"><span id="labelOntology'
			+ id
			+ '"></span></td></tr><tr><td class="areaDetailTitle"><b>Property</b></td><td class="areaDetailCells"><span id="labelProperty'
			+ id
			+ '"></span></td></tr><tr><td class="areaDetailTitle"><b>Formula</b></td><td class="areaDetailCells"><span id="labelFormula'
			+ id
			+ '"></span></td><td class="areaDetailSmiles" rowspan="3"><span id="imageSmiles'
			+ id
			+ '"></span></td></tr><tr><td class="areaDetailTitle"><b>InChiKey</b></td><td class="areaDetailCells"><span id="labelInChiKey'
			+ id
			+ '"></span></td></tr><tr><td class="areaDetailTitle"><b>Smiles</b></td><td class="areaDetailCells"><span id="labelSmiles'
			+ id
			+ '"></span></td></tr></tbody></table><table class="tableGraph"><thead><tr><th class="areaGraphTitleMS">MS</th><th class="areaGraphTitleMSMS">MSMS</th></tr></thead><tbody><tr><td class="areaGraphMS"><div class="jqPlot" id="graphMs'
			+ id
			+ '"></div></td><td class="areaGraphMSMS"><div class="jqPlot" id="graphMsms'
			+ id + '"></div></td></tr></tbody></table></div>';

	jQuery(html_dialog).dialog({
		minWidth : 800,
		position: {my: "center+"+ numX +" center+" + numY, at:"center top", of: window },
		buttons : {
			"Close" : function() {
				$(this).dialog("close");
			}
		},
		close : function() {
			$(this).remove();
		}
	});

}

function getInnerHTML(id, title) {
	var html_dialog = '<div id="metadata-dialog">'
					+ '<div class="boxMetadata">'
					+ '<div class="areaSmiles">'
					+ '<div id="imageSmiles' + id + '"></div>'
					+ '<div id="properties' + id + '"></div>'
					+ '</div>'
					+ '<div class="areaMsms">'
					+ '<div class="labelTagGraph">' + title + '</div>'
					+ '<div class="jqPlot" id="graphMsms' + id + '"></div>'

					+ '</div>'
					+ '<div class="areaChart">' + '<canvas id="barchart'	+ id + '" class="barchartCanvas"></canvas>' + '</div>'
					+ '</div>';

			return html_dialog;
}

function getInnerHTMLtest(id) {
	var html_dialog = '<div id="metadata-dialog">'
			+ '<table class="tableDetail"><tbody>'
			+ '<tr><td class="areaDetailSmiles"><canvas id="barchart'	+ id + '" class="barchartCanvas"></canvas></td></tr>'
			+ '</tbody></table>'
			+ '</div>';
			return html_dialog;
	}




////////////////////////////////////////////////////////////////////////////////
//Smiles Drawer
////////////////////////////////////////////////////////////////////////////////
function drawSmiles(strId, input, options) {
	// Initialize the drawer
	var smilesDrawer = new SmilesDrawer.Drawer(options);

	SmilesDrawer.parse(input, function(tree) {
		// Draw to the canvas
		smilesDrawer.draw(tree, 'smilesDraw' + strId, 'light', false);
		// smilesDrawer.draw(tree, 'smilesDraw2' + strId, 'light', false);
	});
}
