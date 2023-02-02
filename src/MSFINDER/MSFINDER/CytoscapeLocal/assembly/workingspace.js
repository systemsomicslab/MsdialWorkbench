function importingWorkingSpace() {
    var jsonStyle = $.getJSON("./style.json");
    var jsonElements = $.getJSON("./elements.json");
    var aniDur = 500;
    var layoutPadding = 50;
    var easing = 'linear';

    var cy = cytoscape({
        container: document.getElementById('cy'),
        minZoom: 0.01,
        maxZoom: 100,
        wheelSensitivity: 0.1,
        style: jsonStyle,
        elements: jsonElements,
        layout: {
            name: 'cose',
            idealEdgeLength: 100,
            nodeOverlap: 5,
            refresh: 20,
            fit: true,
            padding: 30,
            randomize: false,
            componentSpacing: 100,
            nodeRepulsion: 400000,
            edgeElasticity: 100,
            nestingFactor: 5,
            gravity: 80,
            numIter: 1000,
            initialTemp: 200,
            coolingFactor: 0.95,
            minTemp: 1.0
        },
        ready: function () {
            window.cy = this;
        }
    });

    $('input[name="radio-1"]:radio').change(function () {
        cy.style()
            .selector('node')
            .style({
                'content': 'data(' + $(this).val() + ')'
            })
            .update();

        console.info($(this).val());
    });

    function Off_Elements(e, edges, nodes) {
        $(e).next("span").css({
            "color": "red"
        });
        cy.style().selector(edges).style({
            'visibility': 'hidden'
        }).update();
        cy.style().selector(nodes).style({
            'visibility': 'hidden'
        }).update();
    }

    function On_Elements(e, edges, nodes) {
        $(e).next("span").css({
            "color": "#eff"
        });
        cy.style().selector(edges).style({
            'visibility': 'visible'
        }).update();
        cy.style().selector(nodes).style({
            'visibility': 'visible'
        }).update();
    }

    var allNodes = null;
    var allEles = null;
    var lastHighlighted = null;
    var lastUnhighlighted = null;
    var selectedNode = null;

    function isDirty() {
        return lastHighlighted != null;
    }

    function highlight(node) {
        var oldNhood = lastHighlighted;

        var nhood = lastHighlighted = node.closedNeighborhood();
        var others = lastUnhighlighted = cy.elements().not(nhood);

        console.log("highlight successed");

        var reset = function () {
            cy.batch(function () {
                others.addClass('hidden');
                nhood.removeClass('hidden');
                allEles.removeClass('faded highlighted');
                nhood.addClass('highlighted');

            });

            return Promise.resolve().then(function () {
                if (isDirty()) {
                    return fit();
                } else {
                    return Promise.resolve();
                };
            }).then(function () {
                return Promise.delay(aniDur);
            });
        };

        var runLayout = function () {


            var p = node.position();
            var l = nhood.filter(':visible').makeLayout({
                name: 'concentric',
                fit: false,
                animate: true,
                animationDuration: aniDur,
                animationEasing: easing,
                boundingBox: {
                    x1: p.x - 1,
                    x2: p.x + 1,
                    y1: p.y - 1,
                    y2: p.y + 1
                },
                avoidOverlap: true,
                concentric: function (ele) {
                    if (ele.same(node)) {
                        return 2;
                    } else {
                        return 1;
                    }
                },
                levelWidth: function () {
                    return 1;
                },
                padding: layoutPadding
            });

            var promise = cy.promiseOn('layoutstop');

            l.run();

            return promise;
        };

        var fit = function () {
            return cy.animation({
                fit: {
                    eles: nhood.filter(':visible'),
                    padding: layoutPadding
                },
                easing: easing,
                duration: aniDur
            }).play().promise();
        };

        var showOthersFaded = function () {
            return Promise.delay(250).then(function () {
                cy.batch(function () {
                    others.removeClass('hidden').addClass('faded');
                });
            });
        };

        return Promise.resolve()
            .then(reset)
            .then(runLayout)
            .then(fit)
            .then(showOthersFaded);

    }

    $('#netwk_MsmsCluster').on("click", function () {
        var e = this;
        var edges = cy.edges("[linecolor='red']");
        var msms_edges_only_nodes = cy.nodes("[AllEdgesType='MsmsClustering']");
        if (edges.visible()) {
            Off_Elements(e, edges, msms_edges_only_nodes);
        } else if (edges.hidden()) {
            On_Elements(e, edges, msms_edges_only_nodes);
        }
    });

    $('#netwk_OntologyCluster').on("click", function () {
        var e = this;
        var edges = cy.edges("[linecolor='blue']");
        var ontology_edges_only_nodes = cy.nodes("[AllEdgesType='OntologyClustering']");
        if (edges.visible()) {
            Off_Elements(e, edges, ontology_edges_only_nodes);
        } else if (edges.hidden()) {
            On_Elements(e, edges, ontology_edges_only_nodes);
        }
    });

    $('#netwk_ReactionCluster').on("click", function () {
        var e = this;
        var edges = cy.edges("[linecolor='yellow']");
        var reaction_edges_only_nodes = cy.nodes("[AllEdgesType='ReactionClustering']");
        if (edges.visible()) {
            Off_Elements(e, edges, reaction_edges_only_nodes);
        } else if (edges.hidden()) {
            On_Elements(e, edges, reaction_edges_only_nodes);
        }
    });


    $('#searchButton').on("click", function () {
        var e = this;
        var searchText = document.getElementById('searchBox').value;
        var filteredJsons = {};
        console.log(jsonElements["responseJSON"]["nodes"]);
        var nodes = jsonElements["responseJSON"]["nodes"];
        var edges = jsonElements["responseJSON"]["edges"];

        nodes.forEach(function (node) {
            if (searchText == node["data"]["id"]) {

                var cyNode = cy.nodes("node[id = '" + node["data"]["id"] + "']");
                var pos = cyNode.position();
                cy.animate({
                    fit: {
                        eles: cyNode,
                        padding: 100
                    }
                }, {
                        duration: 100
                    });

            }

        });
    });

    $('#searchBox').keypress( function ( e ) {
    	if ( e.which == 13 ) {
        var e = this;
        var searchText = document.getElementById('searchBox').value;
        var filteredJsons = {};
        console.log(jsonElements["responseJSON"]["nodes"]);
        var nodes = jsonElements["responseJSON"]["nodes"];
        var edges = jsonElements["responseJSON"]["edges"];

        nodes.forEach(function (node) {
            if (searchText == node["data"]["id"]) {

                var cyNode = cy.nodes("node[id = '" + node["data"]["id"] + "']");
                var pos = cyNode.position();
                cy.animate({
                    fit: {
                        eles: cyNode,
                        padding: 100
                    }
                }, {
                        duration: 100
                    });

            }

        });
    	}
    });
    
    

    allNodes = cy.nodes();
    allEles = cy.elements();

    var mspFormat = "";
    var mspFileName = "";

    cy.on('tap', 'node',
        function () {

            var strId = ''
            var numMsMin = 0;
            var numMsmsMin = 0;
            var numX = 300 + Math.floor(Math.random() * 100);
            var numY = 300 + Math.floor(Math.random() * 100);
            var arrayData = '';
            var labelData = '';


            //Make deatil dialog
            strId = this.data('id');

            if (document.getElementById("labelID" + strId) == null) {

                // showDialog(strId, numX, numY);

                document.getElementById("metadata").innerHTML = getInnerHTML(this.data('id'));

            	//Split Smiles 75
                var strSmiles = this.data('Smiles');
                var strHtml = '';
                var pattern = /.{50}/g;
            	var arraySequence = new Array();
            	var numSeqLineCount = 0;
            	var numModSeq = 0;

            	if(strSmiles == 'Unknown' ){
            		strSmiles = '';
            	}
            	
            	if(strSmiles.length > 50){
            		arraySequence = strSmiles.match(pattern);
            		numSeqLineCount = arraySequence.length;
            		numModSeq = (strSmiles.length) % 50;
            		arraySequence[numSeqLineCount] = strSmiles.substr( 0 - numModSeq,numModSeq);
            		for(var numCnt = 0; numCnt <= numSeqLineCount; numCnt++){
            			strHtml = strHtml + arraySequence[numCnt] + '<br>';
            		}
            	}else{
            		strHtml = strSmiles;
            	}

                document.getElementById("properties" + strId).innerHTML = '<div class="areaTag">'
                	+'<div class="labelTag">ID</div>'
                	+'<div class="labelValue">' + this.data('id') + '</div>'
                	+'</div>'
                	+'<div class="areaTag">'
                	+'<div class="labelTag">RT</div>' 
                	+'<div class="labelValue">' + this.data('Rt') + '</div>'
                	+'</div>'
                	+'<div class="areaTag">'
                	+'<div class="labelTag">m/z</div>' 
                	+'<div class="labelValue">' + this.data('Mz') + '</div>'
                	+'</div>'
                	+'<div class="areaTag">'
                	+'<div class="labelTag">Formula</div>' 
                	+'<div class="labelValue">' + this.data('Formula') + '</div>'
                	+'</div>'
                	+'<div class="areaTag">'
                	+'<div class="labelTag">Ontology</div>' 
                	+'<div class="labelValue">' + this.data('Ontology') + '</div>'
                	+'</div>'
                	+'<div class="areaTag">'
                	+'<div class="labelTag">InChIKey</div>' 
                	+'<div class="labelValue">' + this.data('InChiKey') + '</div>'
                	+'</div>'
                	+'<div class="areaTag">'
                	+'<div class="labelTag">SMILES</div>' 
                	+'<div class="labelSmilesValue"><tt>' + strHtml + '</tt></div>'
                	+'</div>';

                mspFileName = this.data('id') + ".msp";
                mspFormat = "Name: " + this.data('id') + "\r\n" +
                    "PRECURSORMZ: " + this.data('Mz') + "\r\n" +
                    "PRECURSORTYPE: " + this.data('Adduct') + "\r\n" +
                    "IONMODE: " + this.data('IonMode') + "\r\n" +
                    "RETENTIONTIME: " + this.data('Rt') + "\r\n" +
                    "Formula: " + this.data('Formula') + "\r\n" +
                    "Ontology: " + this.data('Ontology') + "\r\n" +
                    "InChIKey: " + this.data('InChiKey') + "\r\n" +
                    "SMILES: " + this.data('Smiles') + "\r\n" +
                    "Num Peaks: " + this.data('MSMS').length + "\r\n";

                for (let pair of this.data('MSMS')) {
                    mspFormat += pair[0] + "\t" + pair[1] + "\r\n";
                }



                arrayData = this.data('MSMS');
                labelData = this.data('MsMsLabel');

                numMsmsMin = Math.floor(this.data('MsmsMin') / 100) * 100;
                makeGraph2(arrayData, labelData, 'graphMsms' + strId, numMsmsMin, 100);

                //http://doc.gdb.tools/smilesDrawer/
                var input = this.data('Smiles');
                var options = {
                    width: 420,
                    height: 420
                };

                if (input === 'Unknown' || input ==='') {
                    document.getElementById("imageSmiles" + strId).innerHTML = '<div class="areaNodata"><div>NO DATA</div></div>';
                } else {
                    document.getElementById("imageSmiles" + strId).innerHTML = '<canvas id="smilesDraw' + strId + '" width="480" height="480"><\/canvas>';
                    drawSmiles(strId, input, options);
                }

            }
        });

    $('#downloadButton').click(function () {
        if (mspFileName == "") return;
        var content = mspFormat;
        //var link = document.createElement( 'a' );
        var blob = new Blob([content], {
            "type": "application/octet-stream"
        });

        if (window.navigator.msSaveBlob) {
            window.navigator.msSaveBlob(blob, mspFileName);
            window.navigator.msSaveOrOpenBlob(blob, mspFileName);
        } else {
            document.getElementById("downloadButton").download = mspFileName;
            document.getElementById("downloadButton").href = window.URL.createObjectURL(blob);

        }

    });

}
