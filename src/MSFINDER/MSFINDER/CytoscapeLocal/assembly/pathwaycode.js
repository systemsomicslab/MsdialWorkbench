// photos from flickr with creative commons license
function importingWorkingSpace() {
  var jsonElements = $.getJSON("./LipidNetwork.json");
  var aniDur = 500;
  var layoutPadding = 50;
  var easing = 'linear';

  var cy = cytoscape({
    container: document.getElementById('cypathway'),
    // boxSelectionEnabled: false,
    // autounselectify: true,
    minZoom: 0.01,
    maxZoom: 100,
    wheelSensitivity: 0.1,
    style: cytoscape.stylesheet()
      .selector('node')
      .css({
        'height': 30,
        'width': 30,
        'font-size': 50,
        'background-fit': 'cover',
        'border-color': '#000',
        'border-width': 3,
        'border-opacity': 0.5,
        'shape': 'ellipse',
        'content': 'data(title)',
        'color': 'white'
      })
      .selector('edge')
      .css({
        'curve-style': 'bezier',
        'width': 2,
        'line-color': 'white',
        'target-arrow-color': '#ffaaaa'
      })
      .selector('#MAG')
      .css({
        'background-image': './lipidgraphs/MAG.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#DAG')
      .css({
        'background-image': './lipidgraphs/DAG.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#TAG')
      .css({
        'background-image': './lipidgraphs/TAG.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#LPC')
      .css({
        'background-image': './lipidgraphs/LPC.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#LPE')
      .css({
        'background-image': './lipidgraphs/LPE.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#LPG')
      .css({
        'background-image': './lipidgraphs/LPG.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#LPI')
      .css({
        'background-image': './lipidgraphs/LPI.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#LPS')
      .css({
        'background-image': './lipidgraphs/LPS.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#LPA')
      .css({
        'background-image': './lipidgraphs/LPA.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#LDGTS')
      .css({
        'background-image': './lipidgraphs/LDGTS.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#PC')
      .css({
        'background-image': './lipidgraphs/PC.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#PE')
      .css({
        'background-image': './lipidgraphs/PE.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#PG')
      .css({
        'background-image': './lipidgraphs/PG.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#PI')
      .css({
        'background-image': './lipidgraphs/PI.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#PS')
      .css({
        'background-image': './lipidgraphs/PS.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#PA')
      .css({
        'background-image': './lipidgraphs/PA.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#BMP')
      .css({
        'background-image': './lipidgraphs/BMP.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#HBMP')
      .css({
        'background-image': './lipidgraphs/HBMP.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#CL')
      .css({
        'background-image': './lipidgraphs/CL.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#EtherPC')
      .css({
        'background-image': './lipidgraphs/EtherPC.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#EtherPE')
      .css({
        'background-image': './lipidgraphs/EtherPE.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#OxPC')
      .css({
        'background-image': './lipidgraphs/OxPC.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#OxPE')
      .css({
        'background-image': './lipidgraphs/OxPE.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#OxPG')
      .css({
        'background-image': './lipidgraphs/OxPG.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#OxPI')
      .css({
        'background-image': './lipidgraphs/OxPI.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#OxPS')
      .css({
        'background-image': './lipidgraphs/OxPS.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#EtherOxPC')
      .css({
        'background-image': './lipidgraphs/EtherOxPC.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#EtherOxPE')
      .css({
        'background-image': './lipidgraphs/EtherOxPE.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#PMeOH')
      .css({
        'background-image': './lipidgraphs/PMeOH.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#PEtOH')
      .css({
        'background-image': './lipidgraphs/PEtOH.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#MGDG')
      .css({
        'background-image': './lipidgraphs/MGDG.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#DGDG')
      .css({
        'background-image': './lipidgraphs/DGDG.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#SQDG')
      .css({
        'background-image': './lipidgraphs/SQDG.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#DGTS')
      .css({
        'background-image': './lipidgraphs/DGTS.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#GlcADG')
      .css({
        'background-image': './lipidgraphs/GlcADG.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#AcylGlcADG')
      .css({
        'background-image': './lipidgraphs/AcylGlcADG.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#CE')
      .css({
        'background-image': './lipidgraphs/CE.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#ACar')
      .css({
        'background-image': './lipidgraphs/ACar.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#FA')
      .css({
        'background-image': './lipidgraphs/FA.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#FAHFA')
      .css({
        'background-image': './lipidgraphs/FAHFA.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#SM')
      .css({
        'background-image': './lipidgraphs/SM.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#Cer-ADS')
      .css({
        'background-image': './lipidgraphs/Cer-ADS.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#Cer-AS')
      .css({
        'background-image': './lipidgraphs/Cer-AS.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#Cer-BDS')
      .css({
        'background-image': './lipidgraphs/Cer-BDS.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#Cer-BS')
      .css({
        'background-image': './lipidgraphs/Cer-BS.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#Cer-EODS')
      .css({
        'background-image': './lipidgraphs/Cer-EODS.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#Cer-EOS')
      .css({
        'background-image': './lipidgraphs/Cer-EOS.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#Cer-NDS')
      .css({
        'background-image': './lipidgraphs/Cer-NDS.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#Cer-NS')
      .css({
        'background-image': './lipidgraphs/Cer-NS.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#Cer-NP')
      .css({
        'background-image': './lipidgraphs/Cer-NP.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#Cer-AP')
      .css({
        'background-image': './lipidgraphs/Cer-AP.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#HexCer-NS')
      .css({
        'background-image': './lipidgraphs/HexCer-NS.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#HexCer-NDS')
      .css({
        'background-image': './lipidgraphs/HexCer-NDS.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#HexCer-AP')
      .css({
        'background-image': './lipidgraphs/HexCer-AP.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#SHexCer')
      .css({
        'background-image': './lipidgraphs/SHexCer.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      })
      .selector('#GM3')
      .css({
        'background-image': './lipidgraphs/GM3.png',
        'height': 150,
        'width': 300,
        'shape': 'rectangle'
      }),
    elements: jsonElements,
    layout: {
      name: 'breadthfirst',
      idealEdgeLength: 100,
      nodeOverlap: 1000,
      refresh: 20,
      fit: true,
      padding: 30,
      animate: true,
      randomize: false,
      componentSpacing: 100,
      nodeRepulsion: 2048,
      edgeElasticity: 32,
      nestingFactor: 5,
      gravity: 1,
      numIter: 1000,
      initialTemp: 200,
      coolingFactor: 0.95,
      minTemp: 1.0,
      directed: false,
      padding: 10
    }
  }); // cy init

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

    var reset = function() {
      cy.batch(function() {
        others.addClass('hidden');
        nhood.removeClass('hidden');
        allEles.removeClass('faded highlighted');
        nhood.addClass('highlighted');
          });

      return Promise.resolve().then(function() {
        if (isDirty()) {
          return fit();
        } else {
          return Promise.resolve();
        };
      }).then(function() {
        return Promise.delay(aniDur);
      });
    };

    var runLayout = function() {

      //var cyNode = cy.nodes("node[id = '" + node["data"]["id"] + "']");
      var p = node.position();

      ////filteredJsons.push(node);
      //var selectedNode = cy.elements('node[id = "' + node["data"]["id"] + '"]');
      ////cy.elements('node#1145, edge[source = "1145"]');
      ////console.log(selectedNode);
      //cy.style().selector(selectedNode).style({ 'visibility': 'hidden' }).update();
      //console.log("Successed");
      //// "border-width" : "2px",





      //var p = node.data('orgPos');

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
        concentric: function(ele) {
          if (ele.same(node)) {
            return 2;
          } else {
            return 1;
          }
        },
        levelWidth: function() {
          return 1;
        },
        padding: layoutPadding
      });

      var promise = cy.promiseOn('layoutstop');

      l.run();

      return promise;
    };

    var fit = function() {
      return cy.animation({
        fit: {
          eles: nhood.filter(':visible'),
          padding: layoutPadding
        },
        easing: easing,
        duration: aniDur
      }).play().promise();
    };

    var showOthersFaded = function() {
      return Promise.delay(250).then(function() {
        cy.batch(function() {
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


  allNodes = cy.nodes();
  allEles = cy.elements();

  cy.on('tap', 'node',
    function() {
      console.log("tap unselect successed");
    });


  cy.on('select unselect', 'node', function() {
    console.log("select unselect successed");

    var node = cy.$('node:selected');
    if (node.length > 1) return;
    // selectedNode = node;
    if (node.nonempty()) {
      // showNodeInfo(node);
      Promise.resolve().then(function() {
        return highlight(node);
      });
    } else {
      // hideNodeInfo();
      // clear();
    }

    //}, 100));
  });
}
