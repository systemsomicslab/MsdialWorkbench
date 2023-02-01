(function(){
	console.info("working");
	var myContextMenu = document.getElementById('js-contextmenu');
	document.body.addEventListener('contextmenu',function(e){
		var posX = e.pageX;
		var posY = e.pageY;
		myContextMenu.style.left = posX+'px';
		myContextMenu.style.top = posY+'px';
		myContextMenu.classList.add('show');
	});
	document.body.addEventListener('click',function(){
		if(myContextMenu.classList.contains('show')) {
			myContextMenu.classList.remove('show');
		}
	})
}());